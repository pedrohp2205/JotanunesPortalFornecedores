using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Documents;

public class DocumentDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public long? SupplyRequestId { get; set; }
    public long? WorkSiteId { get; set; }
    public string? WorkSiteName { get; set; }
    public long DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public long UploadedBySupplierUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;

    public string? WorkerName { get; set; }
    public string? WorkerCpf { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public DateOnly? ReferencePeriodStart { get; set; }
    public DateOnly? ReferencePeriodEnd { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    public DocumentStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
