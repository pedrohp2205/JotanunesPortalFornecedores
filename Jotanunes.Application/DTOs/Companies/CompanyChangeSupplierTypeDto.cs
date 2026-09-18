using System.ComponentModel.DataAnnotations;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Companies;

public class CompanyChangeSupplierTypeDto
{
    // Material (1), ManpowerLabor (2) ou os dois (3).
    [Required(ErrorMessage = "Tipo de fornecedor é obrigatório.")]
    public SupplierType SupplierType { get; set; }
}
