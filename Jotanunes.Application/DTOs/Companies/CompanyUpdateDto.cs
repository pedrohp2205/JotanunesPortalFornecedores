using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.Companies;

// O CNPJ não entra aqui de propósito: é a identidade da empresa e não deve
// ser alterado por edição. Uma troca de CNPJ é um novo cadastro.
public class CompanyUpdateDto
{
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
