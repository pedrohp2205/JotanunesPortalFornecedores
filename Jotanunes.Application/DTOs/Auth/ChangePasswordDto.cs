using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Auth;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Senha atual é obrigatória.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "Nova senha deve ter ao menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação da nova senha é obrigatória.")]
    [Compare(nameof(NewPassword), ErrorMessage = "A confirmação não confere com a nova senha.")]
    public string NewPasswordConfirmation { get; set; } = string.Empty;
}
