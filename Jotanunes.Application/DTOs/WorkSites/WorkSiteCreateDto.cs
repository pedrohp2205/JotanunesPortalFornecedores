using System.ComponentModel.DataAnnotations;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.DTOs.WorkSites;

public class WorkSiteCreateDto
{
    [Required(ErrorMessage = "Nome da obra é obrigatório.")]
    public string Name { get; set; } = string.Empty;

    public int RenewalPeriodDays { get; set; } = WorkSite.DefaultRenewalPeriodDays;
}
