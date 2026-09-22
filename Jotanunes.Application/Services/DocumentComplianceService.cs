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

        var data = await LoadData(new[] { supplyRequest });
        return BuildChecklist(supplyRequest, data);
    }

    public async Task<bool> IsOnboardingComplete(long companyId)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            return false;
        }

        var data = new ChecklistData();
        var required = (await GetOnboardingItems(company, data)).Where(i => i.IsRequired).ToList();
        return required.Count > 0 && required.All(i => i.IsSatisfied);
    }

    public async Task<SupplyRequestPendingDto?> GetPending(long supplyRequestId)
    {
        var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(supplyRequestId);
        if (supplyRequest is null)
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }

        var data = await LoadData(new[] { supplyRequest });
        return ToPending(BuildChecklist(supplyRequest, data));
    }

    public async Task<Dictionary<long, SupplyRequestPendingDto>> GetPendingBatch(IReadOnlyCollection<SupplyRequest> supplyRequests)
    {
        var result = new Dictionary<long, SupplyRequestPendingDto>();

        var active = supplyRequests.Where(sr => !sr.IsClosed).ToList();
        if (active.Count == 0)
        {
            return result;
        }

        var data = await LoadData(active);

        foreach (var supplyRequest in active)
        {
            var pending = ToPending(BuildChecklist(supplyRequest, data));
            if (pending is not null)
            {
                result[supplyRequest.Id] = pending;
            }
        }

        return result;
    }

    private static SupplyRequestPendingDto? ToPending(ComplianceChecklistDto checklist)
    {
        var missingOnboarding = checklist.OnboardingItems.Count(i => i.IsRequired && !i.IsSatisfied);
        var missingRecurringCompany = checklist.RecurringCompanyItems.Count(i => i.IsRequired && !i.IsSatisfied);
        var belowTarget = checklist.RequiredWorkerCount.HasValue && checklist.WorkersUpToDate < checklist.RequiredWorkerCount.Value;

        if (missingOnboarding == 0 && missingRecurringCompany == 0 && !belowTarget)
        {
            return null;
        }

        return new SupplyRequestPendingDto
        {
            PeriodStart = checklist.PeriodStart,
            PeriodEnd = checklist.PeriodEnd,
            MissingOnboardingCount = missingOnboarding,
            MissingRecurringCompanyCount = missingRecurringCompany,
            RequiredWorkerCount = checklist.RequiredWorkerCount,
            WorkersUpToDate = checklist.WorkersUpToDate
        };
    }

    private async Task<ChecklistData> LoadData(IReadOnlyCollection<SupplyRequest> supplyRequests)
    {
        var data = new ChecklistData();

        var documents = await _unitOfWork.DocumentRepository.GetBySupplyRequests(supplyRequests.Select(sr => sr.Id).ToList());
        foreach (var group in documents.GroupBy(d => d.SupplyRequestId!.Value))
        {
            data.DocumentsBySupplyRequest[group.Key] = group.ToList();
        }

        foreach (var company in supplyRequests.Select(sr => sr.Company).DistinctBy(c => c.Id))
        {
            data.OnboardingByCompany[company.Id] = await GetOnboardingItems(company, data);
        }

        foreach (var supplierType in supplyRequests.Select(sr => sr.SupplierType).Distinct())
        {
            await GetApplicableTypes(supplierType, data);
        }

        return data;
    }

    private async Task<List<DocumentType>> GetApplicableTypes(SupplierType supplierType, ChecklistData data)
    {
        if (!data.TypesBySupplierType.TryGetValue(supplierType, out var types))
        {
            types = await _unitOfWork.DocumentTypeRepository.GetApplicable(supplierType);
            data.TypesBySupplierType[supplierType] = types;
        }

        return types;
    }

    private async Task<List<ChecklistItemDto>> GetOnboardingItems(Company company, ChecklistData data)
    {
        var applicableTypes = await GetApplicableTypes(company.SupplierType, data);

        var onboardingTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Onboarding).ToList();

        var onboardingDocs = await _unitOfWork.DocumentRepository.GetAll(new DocumentFilter
        {
            CompanyId = company.Id,
            WithoutSupplyRequest = true
        });

        return BuildItems(onboardingTypes, onboardingDocs);
    }

    private static List<ChecklistItemDto> BuildItems(IEnumerable<DocumentType> types, IReadOnlyCollection<Document> documents)
    {
        return types
            .Select(t => ToItem(t, documents.Where(d => d.DocumentTypeId == t.Id)))
            .OrderBy(i => !i.IsRequired)
            .ToList();
    }

    private static ChecklistItemDto ToItem(DocumentType type, IEnumerable<Document> documents)
    {
        var ordered = documents.OrderByDescending(d => d.CreatedAt).ToList();

        var approved = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Approved);
        var pending = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Pending);
        var rejected = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Rejected);

        var (status, current) = approved is not null ? (ChecklistItemStatus.Approved, approved)
            : pending is not null ? (ChecklistItemStatus.Pending, pending)
            : rejected is not null ? (ChecklistItemStatus.Rejected, rejected)
            : (ChecklistItemStatus.NotSent, null);

        return new ChecklistItemDto
        {
            DocumentTypeId = type.Id,
            DocumentTypeCode = type.Code,
            DocumentTypeName = type.Name,
            IsRequired = !type.IsConditional,
            ConditionDescription = type.IsConditional ? type.ConditionDescription : null,
            IsSatisfied = status == ChecklistItemStatus.Approved,
            Status = status,
            StatusDescription = status.ToString(),
            DocumentId = current?.Id,
            RejectionReason = status == ChecklistItemStatus.Rejected ? current!.RejectionReason : null
        };
    }

    private static ComplianceChecklistDto BuildChecklist(SupplyRequest supplyRequest, ChecklistData data)
    {
        var company = supplyRequest.Company;
        var (periodStart, periodEnd) = supplyRequest.WorkSite.GetCurrentPeriod(DateOnly.FromDateTime(DateTime.UtcNow));

        // Os recorrentes seguem o tipo de fornecimento da solicitação; a habilitação, os tipos da empresa.
        var applicableTypes = data.TypesBySupplierType[supplyRequest.SupplierType];

        var recurringCompanyTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Company).ToList();
        var recurringWorkerTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Worker).ToList();

        var onboardingItems = data.OnboardingByCompany[company.Id];

        var recurringDocs = data.DocumentsBySupplyRequest
            .GetValueOrDefault(supplyRequest.Id, [])
            .Where(d => (d.ReferencePeriodEnd == null || d.ReferencePeriodEnd >= periodStart)
                     && (d.ReferencePeriodStart == null || d.ReferencePeriodStart <= periodEnd))
            .ToList();

        var recurringCompanyItems = BuildItems(recurringCompanyTypes, recurringDocs);

        var workers = recurringDocs
            .Where(d => d.WorkerCpf is not null)
            .GroupBy(d => d.WorkerCpf!)
            .Select(group =>
            {
                var items = BuildItems(recurringWorkerTypes, group.ToList());
                var required = items.Where(i => i.IsRequired).ToList();

                return new WorkerComplianceDto
                {
                    WorkerCpf = group.Key,
                    WorkerName = group.OrderByDescending(d => d.CreatedAt).First().WorkerName ?? string.Empty,
                    IsUpToDate = required.Count > 0 && required.All(i => i.IsSatisfied),
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

    private sealed class ChecklistData
    {
        public Dictionary<SupplierType, List<DocumentType>> TypesBySupplierType { get; } = new();
        public Dictionary<long, List<ChecklistItemDto>> OnboardingByCompany { get; } = new();
        public Dictionary<long, List<Document>> DocumentsBySupplyRequest { get; } = new();
    }
}
