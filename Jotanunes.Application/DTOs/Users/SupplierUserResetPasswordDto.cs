using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Users;

public class SupplierUserResetPasswordDto
{
    [Required(ErrorMessage = "Senha provisória é obrigatória.")]
    [MinLength(8, ErrorMessage = "Senha provisória deve ter ao menos 8 caracteres.")]
    public string TemporaryPassword { get; set; } = string.Empty;
}
