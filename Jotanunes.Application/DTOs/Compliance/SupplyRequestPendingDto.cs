namespace Jotanunes.Application.DTOs.Compliance;

public class SupplyRequestPendingDto
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int MissingOnboardingCount { get; set; }
    public int MissingRecurringCompanyCount { get; set; }
    public int? RequiredWorkerCount { get; set; }
    public int WorkersUpToDate { get; set; }
}
