using System.ComponentModel.DataAnnotations;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.SupplyRequests;

public class SupplyRequestCreateDto
{
    [Required(ErrorMessage = "Empresa é obrigatória.")]
    public long CompanyId { get; set; }

    [Required(ErrorMessage = "Obra é obrigatória.")]
    public long WorkSiteId { get; set; }

    // Material (1) ou ManpowerLabor (2). Uma empresa que fornece os dois recebe uma solicitação para cada.
    [Required(ErrorMessage = "Tipo de fornecimento é obrigatório.")]
    public SupplierType SupplierType { get; set; }

    public int? RequiredWorkerCount { get; set; }
}
