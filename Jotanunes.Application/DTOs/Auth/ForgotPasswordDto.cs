using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;
}
