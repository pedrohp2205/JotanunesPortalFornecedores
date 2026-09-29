using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class WorkSiteStepDefinitions(
    DatabaseFixture databaseFixture,
    WorkSiteGivenContext workSiteCtx)
{
    [Given(@"^que existe uma obra cadastrada$")]
    public async Task DadoQueExisteUmaObraCadastrada()
    {
        await using var context = databaseFixture.CreateDbContext();

        var obra = WorkSiteDomainDriver.CriarObraValida();
        context.WorkSites.Add(obra);
        await context.SaveChangesAsync();

        workSiteCtx.IdObraCadastrada = obra.Id;
    }
}
