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

    public async Task<ComplianceChecklistDto> GetChecklist(long companyWorkSiteId, long? companyId = null)
    {
        var companyWorkSite = await _unitOfWork.WorkSiteRepository.GetLinkById(companyWorkSiteId);
        if (companyWorkSite is null || (companyId.HasValue && companyWorkSite.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Solicitação (empresa e obra) não encontrada");
        }

        var company = await _unitOfWork.CompanyRepository.GetById(companyWorkSite.CompanyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        return await BuildChecklist(companyWorkSite, company);
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

    public async Task<List<OverdueCompanyWorkSiteDto>> GetOverdue()
    {
        var links = await _unitOfWork.WorkSiteRepository.GetAllLinks();
        var result = new List<OverdueCompanyWorkSiteDto>();

        foreach (var link in links)
        {
            var checklist = await BuildChecklist(link, link.Company);

            var missingOnboarding = checklist.OnboardingItems.Count(i => !i.IsSatisfied);
            var missingRecurringCompany = checklist.RecurringCompanyItems.Count(i => !i.IsSatisfied);
            var belowTarget = checklist.RequiredWorkerCount.HasValue && checklist.WorkersUpToDate < checklist.RequiredWorkerCount.Value;

            if (missingOnboarding == 0 && missingRecurringCompany == 0 && !belowTarget)
            {
                continue;
            }

            result.Add(new OverdueCompanyWorkSiteDto
            {
                CompanyWorkSiteId = checklist.CompanyWorkSiteId,
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
            });
        }

        return result;
    }

    private async Task<List<ChecklistItemDto>> GetOnboardingItems(Company company)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var applicableTypes = await _unitOfWork.DocumentTypeRepository.GetApplicable(company.SupplierType);
        var onboardingTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Onboarding).ToList();

        var onboardingDocs = await _unitOfWork.DocumentRepository.GetAll(new DocumentFilter
        {
            CompanyId = company.Id,
            Status = DocumentStatus.Approved
        });

        return onboardingTypes.Select(t =>
        {
            var doc = onboardingDocs
                .Where(d => d.DocumentTypeId == t.Id)
                .Where(d => !t.RequiresExpirationDate || !d.ExpirationDate.HasValue || d.ExpirationDate.Value >= today)
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

    private async Task<ComplianceChecklistDto> BuildChecklist(CompanyWorkSite companyWorkSite, Company company)
    {
        var (periodStart, periodEnd) = companyWorkSite.WorkSite.GetCurrentPeriod(DateOnly.FromDateTime(DateTime.UtcNow));

        var applicableTypes = await _unitOfWork.DocumentTypeRepository.GetApplicable(company.SupplierType);

        var recurringCompanyTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Company).ToList();
        var recurringWorkerTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Worker).ToList();

        var onboardingItems = await GetOnboardingItems(company);

        var recurringDocs = await _unitOfWork.DocumentRepository.GetAll(new DocumentFilter
        {
            CompanyWorkSiteId = companyWorkSite.Id,
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
            CompanyWorkSiteId = companyWorkSite.Id,
            CompanyId = company.Id,
            CompanyCorporateName = company.CorporateName,
            WorkSiteId = companyWorkSite.WorkSiteId,
            WorkSiteName = companyWorkSite.WorkSite.Name,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RequiredWorkerCount = companyWorkSite.RequiredWorkerCount,
            WorkersUpToDate = workers.Count(w => w.IsUpToDate),
            OnboardingItems = onboardingItems,
            RecurringCompanyItems = recurringCompanyItems,
            Workers = workers
        };
    }
}
