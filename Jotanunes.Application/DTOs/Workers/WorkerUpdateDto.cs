using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Workers;

public class WorkerUpdateDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; set; } = string.Empty;
}
