using System.ComponentModel.DataAnnotations;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.SupplyRequests;

public class SupplyRequestWithNewCompanyCreateDto
{
    [Required(ErrorMessage = "Dados da empresa são obrigatórios.")]
    public CompanyCreateDto Company { get; set; } = new();

    [Required(ErrorMessage = "Usuário de acesso ao portal é obrigatório.")]
    public SupplierUserCreateDto User { get; set; } = new();

    [Required(ErrorMessage = "Obra é obrigatória.")]
    public long WorkSiteId { get; set; }
    
    [Required(ErrorMessage = "Tipo de fornecimento é obrigatório.")]
    public SupplierType SupplierType { get; set; }

    public int? RequiredWorkerCount { get; set; }
}
