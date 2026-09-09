using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Companies;

public class CompanyCreateDto
{
    [Required(ErrorMessage = "CNPJ é obrigatório.")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "Razão social é obrigatória.")]
    public string CorporateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome fantasia é obrigatório.")]
    public string TradeName { get; set; } = string.Empty;

    public string? StateRegistration { get; set; }

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefone é obrigatório.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do responsável é obrigatório.")]
    public string ResponsibleName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Endereço é obrigatório.")]
    public AddressDto Address { get; set; } = new();
}
