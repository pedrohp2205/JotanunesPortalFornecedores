using Jotanunes.Domain.Entities;

namespace Jotanunes.External.BddTests.Drivers;

public class WorkSiteDomainDriver
{
    public static WorkSite CriarObraValida(string? nome = null)
    {
        return new WorkSite(nome ?? "Obra Teste");
    }
}
