using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

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
