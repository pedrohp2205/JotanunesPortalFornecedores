namespace Jotanunes.Application.DTOs.Auth;

public class AuthenticatedUserDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public string CompanyCnpj { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public DateTime? LastAccessAt { get; set; }
}
