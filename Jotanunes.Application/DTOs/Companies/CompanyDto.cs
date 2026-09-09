using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Companies;

public class CompanyDto
{
    public long Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string FormattedCnpj { get; set; } = string.Empty;
    public string CorporateName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? StateRegistration { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ResponsibleName { get; set; } = string.Empty;
    public CompanyStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
