namespace Jotanunes.Application.DTOs.WorkSites;

public class WorkSiteCompanyDto
{
    public long CompanyId { get; set; }
    public string CorporateName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string FormattedCnpj { get; set; } = string.Empty;
    public int? RequiredWorkerCount { get; set; }
}
