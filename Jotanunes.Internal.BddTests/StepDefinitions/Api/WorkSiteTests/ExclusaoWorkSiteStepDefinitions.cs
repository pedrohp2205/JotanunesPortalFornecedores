using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.WorkSiteTests;

[Binding]
internal class ExclusaoWorkSiteStepDefinitions(
    WorkSiteApiClientFixture workSiteFix,
    WorkSiteGivenContext workSiteCtx,
    HttpResponseContext httpResponseCtx)
{
    [When(@"eu excluir a obra cadastrada")]
    public async Task QuandoEuExcluirAObraCadastrada()
    {
        await workSiteFix.DeleteAsync(workSiteCtx.IdObraCadastrada!.Value);
    }

    [When(@"eu excluir uma obra inexistente")]
    public async Task QuandoEuExcluirUmaObraInexistente()
    {
        await workSiteFix.DeleteAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a obra cadastrada não deve mais ser encontrada")]
    public async Task EntaoAObraCadastradaNaoDeveMaisSerEncontrada()
    {
        await workSiteFix.GetByIdAsync(workSiteCtx.IdObraCadastrada!.Value);
        ((int)httpResponseCtx.Response!.StatusCode).Should().Be(StatusCodes.Status404NotFound);
    }
}
