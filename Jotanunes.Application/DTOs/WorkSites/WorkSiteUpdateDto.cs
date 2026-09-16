using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Application.DTOs.WorkSites;

public class WorkSiteUpdateDto
{
    [Required(ErrorMessage = "Nome da obra é obrigatório.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Período de renovação deve ser maior que zero.")]
    public int RenewalPeriodDays { get; set; }
}
