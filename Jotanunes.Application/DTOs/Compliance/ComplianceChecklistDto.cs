using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Compliance;

public class ComplianceChecklistDto
{
    public long SupplyRequestId { get; set; }
    public SupplierType SupplierType { get; set; }
    public SupplyRequestStatus Status { get; set; }
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public long WorkSiteId { get; set; }
    public string WorkSiteName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int? RequiredWorkerCount { get; set; }
    public int WorkersUpToDate { get; set; }
    public List<ChecklistItemDto> OnboardingItems { get; set; } = [];
    public List<ChecklistItemDto> RecurringCompanyItems { get; set; } = [];
    public List<WorkerComplianceDto> Workers { get; set; } = [];
}
