using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Users;

public class SupplierUserCreateDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha provisória é obrigatória.")]
    [MinLength(8, ErrorMessage = "Senha provisória deve ter ao menos 8 caracteres.")]
    public string TemporaryPassword { get; set; } = string.Empty;
}
