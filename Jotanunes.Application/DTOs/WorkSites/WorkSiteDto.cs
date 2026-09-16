namespace Jotanunes.Application.DTOs.WorkSites;

public class WorkSiteDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RenewalPeriodDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
