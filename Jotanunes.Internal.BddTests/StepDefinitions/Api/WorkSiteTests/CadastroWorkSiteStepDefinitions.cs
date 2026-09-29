using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.WorkSiteTests;

[Binding]
internal class CadastroWorkSiteStepDefinitions(
    WorkSiteApiClientFixture workSiteFix,
    WorkSiteResultContext workSiteResultCtx)
{
    [When(@"^eu cadastrar a obra ""(.*)"" com renovação a cada (\d+) dias$")]
    public async Task QuandoEuCadastrarAObra(string nome, int periodoRenovacaoDias)
    {
        workSiteResultCtx.Obra = await workSiteFix.CreateAsync(WorkSiteDomainDriver.CriarWorkSiteCreateDto(nome, periodoRenovacaoDias));
    }
}
