using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class SupplyRequestFilter
{
    public string? CompanyName { get; set; }
    public string? WorkSiteName { get; set; }
    public SupplierType? SupplierType { get; set; }
    public SupplyRequestStatus? Status { get; set; }
    public bool? HasPending { get; set; }
}
