namespace Jotanunes.Application.DTOs.Compliance;

public class ChecklistItemDto
{
    public long DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public long? DocumentId { get; set; }
}
