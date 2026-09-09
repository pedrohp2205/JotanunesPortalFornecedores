using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Auth;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token é obrigatório.")]
    public string RefreshToken { get; set; } = string.Empty;
}
