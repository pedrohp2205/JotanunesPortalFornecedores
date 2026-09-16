using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Documents;

public class DocumentRejectDto
{
    [Required(ErrorMessage = "Motivo da rejeição é obrigatório.")]
    public string Reason { get; set; } = string.Empty;
}
