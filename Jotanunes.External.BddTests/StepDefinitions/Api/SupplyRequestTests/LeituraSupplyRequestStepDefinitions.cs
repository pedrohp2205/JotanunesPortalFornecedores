using AwesomeAssertions;

using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.Infra.IoC.Authorization;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class LeituraSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    HttpResponseContext httpResponseCtx)
{
    [When(@"eu listar as solicitações da minha empresa")]
    public async Task QuandoEuListarAsSolicitacoesDaMinhaEmpresa()
    {
        await supplyRequestFix.GetAsync();
    }

    [When(@"eu consultar uma solicitação inexistente")]
    public async Task QuandoEuConsultarUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a resposta indica que é preciso trocar a senha provisória")]
    public async Task EntaoARespostaIndicaQueEPrecisoTrocarASenhaProvisoria()
    {
        var error = await httpResponseCtx.TryReadErrorAsync();
        error.Should().NotBeNull();
        error!.MustChangePassword.Should().BeTrue();
        error.Message.Should().Be(PasswordChangedRequirement.Message);
    }
}
