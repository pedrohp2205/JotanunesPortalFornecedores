namespace Jotanunes.Application.DTOs.Compliance;

public class ChecklistItemDto
{
    public long DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;
    public string? ConditionDescription { get; set; }

    public bool IsSatisfied { get; set; }
    public ChecklistItemStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;

    public long? DocumentId { get; set; }
    public string? RejectionReason { get; set; }
}
