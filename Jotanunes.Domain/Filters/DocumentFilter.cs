using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class DocumentFilter
{
    public long? CompanyId { get; set; }
    public long? WorkSiteId { get; set; }
    public long? SupplyRequestId { get; set; }
    public long? DocumentTypeId { get; set; }
    public bool? WithoutSupplyRequest { get; set; }
    public DocumentStatus? Status { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
}
