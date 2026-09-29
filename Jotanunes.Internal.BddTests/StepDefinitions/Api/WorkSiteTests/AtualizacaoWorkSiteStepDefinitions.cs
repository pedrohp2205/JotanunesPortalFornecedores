using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.WorkSiteTests;

[Binding]
internal class AtualizacaoWorkSiteStepDefinitions(
    WorkSiteApiClientFixture workSiteFix,
    WorkSiteGivenContext workSiteCtx,
    WorkSiteResultContext workSiteResultCtx)
{
    [When(@"^eu atualizar a obra cadastrada para ""(.*)"" com renovação a cada (\d+) dias$")]
    public async Task QuandoEuAtualizarAObraCadastrada(string nome, int periodoRenovacaoDias)
    {
        workSiteResultCtx.Obra = await workSiteFix.UpdateAsync(
            workSiteCtx.IdObraCadastrada!.Value,
            WorkSiteDomainDriver.CriarWorkSiteUpdateDto(nome, periodoRenovacaoDias));
    }

    [When(@"eu atualizar uma obra inexistente")]
    public async Task QuandoEuAtualizarUmaObraInexistente()
    {
        await workSiteFix.UpdateAsync(TestConstants.ID_INEXISTENTE, WorkSiteDomainDriver.CriarWorkSiteUpdateDto("Obra", 30));
    }
}
