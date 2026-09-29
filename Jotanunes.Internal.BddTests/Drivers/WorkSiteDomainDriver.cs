using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Internal.BddTests.Drivers;

public class WorkSiteDomainDriver
{
    public static WorkSite CriarObraValida(string? nome = null, int periodoRenovacaoDias = WorkSite.DefaultRenewalPeriodDays)
    {
        return new WorkSite(nome ?? "Obra Teste", periodoRenovacaoDias);
    }

    public static WorkSiteCreateDto CriarWorkSiteCreateDto(string nome, int periodoRenovacaoDias)
    {
        return new WorkSiteCreateDto { Name = nome, RenewalPeriodDays = periodoRenovacaoDias };
    }

    public static WorkSiteUpdateDto CriarWorkSiteUpdateDto(string nome, int periodoRenovacaoDias)
    {
        return new WorkSiteUpdateDto { Name = nome, RenewalPeriodDays = periodoRenovacaoDias };
    }
}
