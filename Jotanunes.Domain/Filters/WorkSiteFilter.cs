using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class WorkSiteFilter
{
    public string Name { get; set; } = string.Empty;
    public int? RenewalPeriodDays { get; set; }
    public WorkSiteOrderBy? OrderBy { get; set; }
    public OrderDirection? OrderDirection { get; set; }
}
