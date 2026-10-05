namespace Jotanunes.Application.DTOs.Documents;

public class DocumentUploadDto
{
    public long DocumentTypeId { get; set; }
    public long? SupplyRequestId { get; set; }
    public long? WorkerId { get; set; }
    public DateOnly? ReferencePeriodStart { get; set; }
    public DateOnly? ReferencePeriodEnd { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}
