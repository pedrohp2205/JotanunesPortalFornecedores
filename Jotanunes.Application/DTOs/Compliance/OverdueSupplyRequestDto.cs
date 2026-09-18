using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Compliance;

public class OverdueSupplyRequestDto
{
    public long SupplyRequestId { get; set; }
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public long WorkSiteId { get; set; }
    public string WorkSiteName { get; set; } = string.Empty;
    public SupplierType SupplierType { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int MissingOnboardingCount { get; set; }
    public int MissingRecurringCompanyCount { get; set; }
    public int? RequiredWorkerCount { get; set; }
    public int WorkersUpToDate { get; set; }
}
