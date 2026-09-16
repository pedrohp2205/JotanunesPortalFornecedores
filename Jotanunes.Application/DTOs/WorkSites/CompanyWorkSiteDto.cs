namespace Jotanunes.Application.DTOs.WorkSites;

public class CompanyWorkSiteDto
{
    public long CompanyWorkSiteId { get; set; }
    public long WorkSiteId { get; set; }
    public string WorkSiteName { get; set; } = string.Empty;
    public int RenewalPeriodDays { get; set; }
    public int? RequiredWorkerCount { get; set; }
}
