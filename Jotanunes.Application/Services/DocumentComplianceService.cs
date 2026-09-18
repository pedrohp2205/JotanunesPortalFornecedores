using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services;

public class DocumentComplianceService : IDocumentComplianceService
{
    private readonly IUnitOfWork _unitOfWork;

    public DocumentComplianceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ComplianceChecklistDto> GetChecklist(long supplyRequestId, long? companyId = null)
    {
        var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(supplyRequestId);
        if (supplyRequest is null || (companyId.HasValue && supplyRequest.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }

        return await BuildChecklist(supplyRequest, supplyRequest.Company);
    }

    public async Task<bool> IsOnboardingComplete(long companyId)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            return false;
        }

        var onboardingItems = await GetOnboardingItems(company);
        return onboardingItems.Count > 0 && onboardingItems.All(i => i.IsSatisfied);
    }

    // Só solicitações ativas (abertas ou em andamento) entram nos atrasados.
    public async Task<List<OverdueSupplyRequestDto>> GetOverdue()
    {
        var supplyRequests = await _unitOfWork.SupplyRequestRepository.GetActive();
        var result = new List<OverdueSupplyRequestDto>();

        foreach (var supplyRequest in supplyRequests)
        {
            var checklist = await BuildChecklist(supplyRequest, supplyRequest.Company);

            var pending = ToPending(checklist);
            if (pending is not null)
            {
                result.Add(pending);
            }
        }

        return result;
    }

    public async Task<OverdueSupplyRequestDto?> GetPending(long supplyRequestId)
    {
        var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(supplyRequestId);
        if (supplyRequest is null)
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }

        var checklist = await BuildChecklist(supplyRequest, supplyRequest.Company);
        return ToPending(checklist);
    }

    // Critério único de pendência, usado nos atrasados e na conclusão da solicitação.
    private static OverdueSupplyRequestDto? ToPending(ComplianceChecklistDto checklist)
    {
        var missingOnboarding = checklist.OnboardingItems.Count(i => !i.IsSatisfied);
        var missingRecurringCompany = checklist.RecurringCompanyItems.Count(i => !i.IsSatisfied);
        var belowTarget = checklist.RequiredWorkerCount.HasValue && checklist.WorkersUpToDate < checklist.RequiredWorkerCount.Value;

        if (missingOnboarding == 0 && missingRecurringCompany == 0 && !belowTarget)
        {
            return null;
        }

        return new OverdueSupplyRequestDto
        {
            SupplyRequestId = checklist.SupplyRequestId,
            SupplierType = checklist.SupplierType,
            CompanyId = checklist.CompanyId,
            CompanyCorporateName = checklist.CompanyCorporateName,
            WorkSiteId = checklist.WorkSiteId,
            WorkSiteName = checklist.WorkSiteName,
            PeriodStart = checklist.PeriodStart,
            PeriodEnd = checklist.PeriodEnd,
            MissingOnboardingCount = missingOnboarding,
            MissingRecurringCompanyCount = missingRecurringCompany,
            RequiredWorkerCount = checklist.RequiredWorkerCount,
            WorkersUpToDate = checklist.WorkersUpToDate
        };
    }

    private async Task<List<ChecklistItemDto>> GetOnboardingItems(Company company)
    {
        var applicableTypes = await _unitOfWork.DocumentTypeRepository.GetApplicable(company.SupplierType);
        
        var onboardingTypes = applicableTypes
            .Where(t => t.Category == DocumentCategory.Onboarding && !t.IsConditional)
            .ToList();

        var onboardingDocs = await _unitOfWork.DocumentRepository.GetAll(new DocumentFilter
        {
            CompanyId = company.Id,
            Status = DocumentStatus.Approved
        });

        return onboardingTypes.Select(t =>
        {
            var doc = onboardingDocs
                .Where(d => d.DocumentTypeId == t.Id)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

            return new ChecklistItemDto
            {
                DocumentTypeId = t.Id,
                DocumentTypeCode = t.Code,
                DocumentTypeName = t.Name,
                IsSatisfied = doc is not null,
                DocumentId = doc?.Id
            };
        }).ToList();
    }

    private async Task<ComplianceChecklistDto> BuildChecklist(SupplyRequest supplyRequest, Company company)
    {
        var (periodStart, periodEnd) = supplyRequest.WorkSite.GetCurrentPeriod(DateOnly.FromDateTime(DateTime.UtcNow));

        // Os recorrentes seguem o tipo de fornecimento da solicitação; a habilitação, os tipos da empresa.
        var applicableTypes = await _unitOfWork.DocumentTypeRepository.GetApplicable(supplyRequest.SupplierType);

        var recurringCompanyTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Company).ToList();
        var recurringWorkerTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Worker).ToList();

        var onboardingItems = await GetOnboardingItems(company);

        var recurringDocs = await _unitOfWork.DocumentRepository.GetAll(new DocumentFilter
        {
            SupplyRequestId = supplyRequest.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        var recurringCompanyItems = recurringCompanyTypes.Select(t =>
        {
            var doc = recurringDocs.FirstOrDefault(d => d.DocumentTypeId == t.Id && d.Status == DocumentStatus.Approved);
            return new ChecklistItemDto
            {
                DocumentTypeId = t.Id,
                DocumentTypeCode = t.Code,
                DocumentTypeName = t.Name,
                IsSatisfied = doc is not null,
                DocumentId = doc?.Id
            };
        }).ToList();

        var workers = recurringDocs
            .Where(d => d.WorkerCpf is not null)
            .GroupBy(d => d.WorkerCpf!)
            .Select(group =>
            {
                var items = recurringWorkerTypes.Select(t =>
                {
                    var doc = group.FirstOrDefault(d => d.DocumentTypeId == t.Id && d.Status == DocumentStatus.Approved);
                    return new ChecklistItemDto
                    {
                        DocumentTypeId = t.Id,
                        DocumentTypeCode = t.Code,
                        DocumentTypeName = t.Name,
                        IsSatisfied = doc is not null,
                        DocumentId = doc?.Id
                    };
                }).ToList();

                return new WorkerComplianceDto
                {
                    WorkerCpf = group.Key,
                    WorkerName = group.OrderByDescending(d => d.CreatedAt).First().WorkerName ?? string.Empty,
                    IsUpToDate = recurringWorkerTypes.Count > 0 && items.All(i => i.IsSatisfied),
                    Items = items
                };
            })
            .OrderBy(w => w.WorkerName)
            .ToList();

        return new ComplianceChecklistDto
        {
            SupplyRequestId = supplyRequest.Id,
            SupplierType = supplyRequest.SupplierType,
            Status = supplyRequest.Status,
            CompanyId = company.Id,
            CompanyCorporateName = company.CorporateName,
            WorkSiteId = supplyRequest.WorkSiteId,
            WorkSiteName = supplyRequest.WorkSite.Name,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RequiredWorkerCount = supplyRequest.RequiredWorkerCount,
            WorkersUpToDate = workers.Count(w => w.IsUpToDate),
            OnboardingItems = onboardingItems,
            RecurringCompanyItems = recurringCompanyItems,
            Workers = workers
        };
    }
}
