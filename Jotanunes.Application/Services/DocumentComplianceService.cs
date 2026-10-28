using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Time;

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
        var documents = await _unitOfWork.DocumentRepository.GetOnboardingByCompanies(new[] { company.Id });
        var required = (await GetOnboardingItems(company, documents, data)).Where(i => i.IsRequired).ToList();
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

    // Alimenta a tela em que o operacional monta a solicitação: todos os tipos
    // aplicáveis, já marcando o que a empresa entregou na habilitação.
    public async Task<DocumentOptionsDto> GetDocumentOptions(long companyId, SupplierType supplierType, long? supplyRequestId = null)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        JotanunesException.When(
            supplierType is not (SupplierType.Material or SupplierType.ManpowerLabor),
            "Tipo de fornecimento deve ser Material ou Mão de obra.");

        JotanunesException.When(
            (company.SupplierType & supplierType) == 0,
            "A empresa não está cadastrada para fornecer este tipo de serviço.");

        HashSet<long>? selectedTypeIds = null;
        if (supplyRequestId.HasValue)
        {
            var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(supplyRequestId.Value);
            if (supplyRequest is null || supplyRequest.CompanyId != companyId)
            {
                throw new KeyNotFoundException("Solicitação não encontrada");
            }

            var requirements = await _unitOfWork.SupplyRequestRepository.GetRequirements(new[] { supplyRequest.Id });
            selectedTypeIds = requirements.Select(r => r.DocumentTypeId).ToHashSet();
        }

        var data = new ChecklistData();
        var applicableTypes = await GetApplicableTypes(supplierType, data);
        var onboardingDocs = await _unitOfWork.DocumentRepository.GetOnboardingByCompanies(new[] { companyId });
        var onboardingItems = (await GetOnboardingItems(company, onboardingDocs, data)).ToDictionary(i => i.DocumentTypeId);

        List<DocumentOptionDto> Options(DocumentCategory category, DocumentSubject subject)
        {
            return applicableTypes
                .Where(t => t.Category == category && t.Subject == subject)
                .Select(t => ToOption(t, onboardingItems.GetValueOrDefault(t.Id), selectedTypeIds))
                .ToList();
        }

        return new DocumentOptionsDto
        {
            SupplyRequestId = supplyRequestId,
            CompanyId = company.Id,
            CompanyCorporateName = company.CorporateName,
            SupplierType = supplierType,
            CompanyOnboarding = Options(DocumentCategory.Onboarding, DocumentSubject.Company),
            CompanyRecurring = Options(DocumentCategory.Recurring, DocumentSubject.Company),
            WorkerOnboarding = Options(DocumentCategory.Onboarding, DocumentSubject.Worker),
            WorkerRecurring = Options(DocumentCategory.Recurring, DocumentSubject.Worker)
        };
    }

    // A habilitação é o único balde que a empresa carrega de uma solicitação
    // para outra, então é o único em que "já tem" faz sentido.
    private static DocumentOptionDto ToOption(DocumentType type, ChecklistItemDto? onboardingItem, HashSet<long>? selectedTypeIds)
    {
        var alreadyHas = onboardingItem?.IsSatisfied ?? false;
        var suggested = !type.IsConditional && !alreadyHas;

        return new DocumentOptionDto
        {
            DocumentTypeId = type.Id,
            DocumentTypeCode = type.Code,
            DocumentTypeName = type.Name,
            Category = type.Category,
            Subject = type.Subject,
            IsConditional = type.IsConditional,
            ConditionDescription = type.ConditionDescription,
            Selected = selectedTypeIds?.Contains(type.Id) ?? suggested,
            Suggested = suggested,
            CompanyAlreadyHas = alreadyHas,
            CompanyStatus = onboardingItem?.Status ?? ChecklistItemStatus.NotSent,
            CompanyDocumentId = onboardingItem?.DocumentId,
            CompanyDocumentExpirationDate = onboardingItem?.ExpirationDate
        };
    }

    private async Task<ChecklistData> LoadData(IReadOnlyCollection<SupplyRequest> supplyRequests)
    {
        var data = new ChecklistData();
        var today = BrazilTime.Today();

        var earliestPeriodStart = supplyRequests.Min(sr => sr.WorkSite.GetCurrentPeriod(today).Start);

        var requirements = await _unitOfWork.SupplyRequestRepository.GetRequirements(supplyRequests.Select(sr => sr.Id).ToList());
        foreach (var group in requirements.GroupBy(r => r.SupplyRequestId))
        {
            data.RequiredTypeIdsBySupplyRequest[group.Key] = group.Select(r => r.DocumentTypeId).ToHashSet();
        }

        var documents = await _unitOfWork.DocumentRepository.GetBySupplyRequests(
            supplyRequests.Select(sr => sr.Id).ToList(),
            earliestPeriodStart);
        foreach (var group in documents.GroupBy(d => d.SupplyRequestId!.Value))
        {
            data.DocumentsBySupplyRequest[group.Key] = group.ToList();
        }

        var companies = supplyRequests.Select(sr => sr.Company).DistinctBy(c => c.Id).ToList();
        var onboardingDocs = await _unitOfWork.DocumentRepository.GetOnboardingByCompanies(companies.Select(c => c.Id).ToList());
        var onboardingDocsByCompany = onboardingDocs.ToLookup(d => d.CompanyId);

        foreach (var company in companies)
        {
            data.OnboardingByCompany[company.Id] = await GetOnboardingItems(company, onboardingDocsByCompany[company.Id].ToList(), data);
        }

        foreach (var supplierType in supplyRequests.Select(sr => sr.SupplierType).Distinct())
        {
            await GetApplicableTypes(supplierType, data);
        }

        var allocations = await _unitOfWork.WorkerAllocationRepository.GetActiveBySupplyRequests(
            supplyRequests.Select(sr => sr.Id).ToList());
        foreach (var group in allocations.GroupBy(a => a.SupplyRequestId))
        {
            data.AllocationsBySupplyRequest[group.Key] = group.ToList();
        }

        var workerIds = allocations.Select(a => a.WorkerId).Distinct().ToList();
        if (workerIds.Count > 0)
        {
            var workerOnboardingDocs = await _unitOfWork.DocumentRepository.GetOnboardingByWorkers(workerIds);
            foreach (var group in workerOnboardingDocs.GroupBy(d => d.WorkerId!.Value))
            {
                data.OnboardingByWorker[group.Key] = group.ToList();
            }
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

    private async Task<List<ChecklistItemDto>> GetOnboardingItems(Company company, IReadOnlyCollection<Document> onboardingDocs, ChecklistData data)
    {
        var applicableTypes = await GetApplicableTypes(company.SupplierType, data);

        var onboardingTypes = applicableTypes.Where(t => t.Category == DocumentCategory.Onboarding && t.Subject == DocumentSubject.Company).ToList();

        return BuildItems(onboardingTypes, onboardingDocs);
    }

    private static List<ChecklistItemDto> BuildItems(IEnumerable<DocumentType> types, IReadOnlyCollection<Document> documents, DateOnly? expirationReference = null)
    {
        return types
            .Select(t => ToItem(t, documents.Where(d => d.DocumentTypeId == t.Id), expirationReference))
            .OrderBy(i => !i.IsRequired)
            .ToList();
    }

    private static ChecklistItemDto ToItem(DocumentType type, IEnumerable<Document> documents, DateOnly? expirationReference)
    {
        var ordered = documents.OrderByDescending(d => d.CreatedAt).ToList();

        bool IsExpired(Document d) => expirationReference.HasValue && d.IsExpired(expirationReference.Value);

        var approved = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Approved && !IsExpired(d));
        var pending = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Pending);
        var expired = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Approved && IsExpired(d));
        var rejected = ordered.FirstOrDefault(d => d.Status == DocumentStatus.Rejected);

        var (status, current) = approved is not null ? (ChecklistItemStatus.Approved, approved)
            : pending is not null ? (ChecklistItemStatus.Pending, pending)
            : expired is not null ? (ChecklistItemStatus.Expired, expired)
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
            ExpirationDate = current?.ExpirationDate,
            RejectionReason = status == ChecklistItemStatus.Rejected ? current!.RejectionReason : null
        };
    }

    private static ComplianceChecklistDto BuildChecklist(SupplyRequest supplyRequest, ChecklistData data)
    {
        var company = supplyRequest.Company;
        var today = BrazilTime.Today();
        var (periodStart, periodEnd) = supplyRequest.WorkSite.GetCurrentPeriod(today);

        // Só entra no checklist o que a Jotanunes exigiu desta solicitação.
        var applicableTypes = data.TypesBySupplierType[supplyRequest.SupplierType];
        var requiredTypeIds = data.RequiredTypeIdsBySupplyRequest.GetValueOrDefault(supplyRequest.Id, []);
        var requiredTypes = applicableTypes.Where(t => requiredTypeIds.Contains(t.Id)).ToList();

        var recurringCompanyTypes = requiredTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Company).ToList();
        var recurringWorkerTypes = requiredTypes.Where(t => t.Category == DocumentCategory.Recurring && t.Subject == DocumentSubject.Worker).ToList();
        var onboardingWorkerTypes = requiredTypes.Where(t => t.Category == DocumentCategory.Onboarding && t.Subject == DocumentSubject.Worker).ToList();

        var onboardingCompanyTypeIds = requiredTypes
            .Where(t => t.Category == DocumentCategory.Onboarding && t.Subject == DocumentSubject.Company)
            .Select(t => t.Id)
            .ToHashSet();

        var onboardingItems = data.OnboardingByCompany[company.Id]
            .Where(i => onboardingCompanyTypeIds.Contains(i.DocumentTypeId))
            .ToList();

        var recurringDocs = data.DocumentsBySupplyRequest
            .GetValueOrDefault(supplyRequest.Id, [])
            .Where(d => (d.ReferencePeriodEnd == null || d.ReferencePeriodEnd >= periodStart)
                     && (d.ReferencePeriodStart == null || d.ReferencePeriodStart <= periodEnd))
            .ToList();

        var recurringCompanyItems = BuildItems(recurringCompanyTypes, recurringDocs);

        var workers = data.AllocationsBySupplyRequest
            .GetValueOrDefault(supplyRequest.Id, [])
            .Select(allocation =>
            {
                var worker = allocation.Worker;
                var onboardingItems = BuildItems(
                    onboardingWorkerTypes,
                    data.OnboardingByWorker.GetValueOrDefault(worker.Id, []),
                    today);
                var items = BuildItems(recurringWorkerTypes, recurringDocs.Where(d => d.WorkerId == worker.Id).ToList());
                var required = onboardingItems.Concat(items).Where(i => i.IsRequired).ToList();

                return new WorkerComplianceDto
                {
                    WorkerId = worker.Id,
                    WorkerCpf = worker.Cpf,
                    WorkerName = worker.Name,
                    AllocatedAt = allocation.AllocatedAt,
                    IsUpToDate = required.Count > 0 && required.All(i => i.IsSatisfied),
                    OnboardingItems = onboardingItems,
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
            AllocatedWorkerCount = workers.Count,
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
        public Dictionary<long, List<WorkerAllocation>> AllocationsBySupplyRequest { get; } = new();
        public Dictionary<long, List<Document>> OnboardingByWorker { get; } = new();
        public Dictionary<long, HashSet<long>> RequiredTypeIdsBySupplyRequest { get; } = new();
    }
}
