using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.DocumentTypes;

public class SupplierDocumentTypeDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public string CategoryDescription { get; set; } = string.Empty;
    public DocumentSubject Subject { get; set; }
    public string SubjectDescription { get; set; } = string.Empty;
    public SupplierType AppliesTo { get; set; }
    public string AppliesToDescription { get; set; } = string.Empty;
    public bool RequiresExpirationDate { get; set; }
    public bool IsConditional { get; set; }
    public string? ConditionDescription { get; set; }
}
