namespace Jotanunes.Application.DTOs.Documents;

public class DocumentUploadDto
{
    public long DocumentTypeId { get; set; }
    public long? CompanyWorkSiteId { get; set; }
    public string? WorkerName { get; set; }
    public string? WorkerCpf { get; set; }
    public DateOnly? ReferencePeriodStart { get; set; }
    public DateOnly? ReferencePeriodEnd { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}
