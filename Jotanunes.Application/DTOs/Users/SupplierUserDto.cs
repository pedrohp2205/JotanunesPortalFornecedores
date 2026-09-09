namespace Jotanunes.Application.DTOs.Users;

public class SupplierUserDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
