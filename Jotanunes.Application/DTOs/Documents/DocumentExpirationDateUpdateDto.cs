using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Documents;

public class DocumentExpirationDateUpdateDto
{
    [Required(ErrorMessage = "Data de validade é obrigatória.")]
    public DateOnly? ExpirationDate { get; set; }
}
