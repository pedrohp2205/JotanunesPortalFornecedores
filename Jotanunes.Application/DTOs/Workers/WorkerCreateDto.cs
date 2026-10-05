using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Workers;

public class WorkerCreateDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPF é obrigatório.")]
    public string Cpf { get; set; } = string.Empty;
}
