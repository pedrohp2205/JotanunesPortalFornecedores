using System.ComponentModel.DataAnnotations;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.DocumentTypes;

public class DocumentTypeCreateDto
{
    [Required(ErrorMessage = "Código é obrigatório.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; set; } = string.Empty;

    public DocumentCategory Category { get; set; }
    public SupplierType AppliesTo { get; set; }
    public DocumentSubject Subject { get; set; }
    public bool RequiresExpirationDate { get; set; }
    public bool IsConditional { get; set; }
    public string? ConditionDescription { get; set; }
}
