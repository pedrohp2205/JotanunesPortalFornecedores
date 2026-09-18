namespace Jotanunes.API.External.Models;

public class DocumentUploadForm
{
    public long DocumentTypeId { get; set; }
    public long? SupplyRequestId { get; set; }
    public string? WorkerName { get; set; }
    public string? WorkerCpf { get; set; }
    public DateOnly? ReferencePeriodStart { get; set; }
    public DateOnly? ReferencePeriodEnd { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public IFormFile File { get; set; } = null!;
}
