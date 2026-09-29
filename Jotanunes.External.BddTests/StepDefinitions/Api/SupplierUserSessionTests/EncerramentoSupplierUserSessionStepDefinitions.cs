using AwesomeAssertions;

using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserSessionTests;

[Binding]
internal class EncerramentoSupplierUserSessionStepDefinitions(
    AuthApiClientFixture authFix,
    HttpResponseContext httpResponseCtx)
{
    [Given(@"que eu encerrei a minha sessão")]
    public async Task DadoQueEuEncerreiAMinhaSessao()
    {
        await authFix.LogoutAsync();
        httpResponseCtx.Response!.IsSuccessStatusCode.Should().BeTrue();
    }

    [When(@"eu encerrar a minha sessão")]
    public async Task QuandoEuEncerrarAMinhaSessao()
    {
        await authFix.LogoutAsync();
    }
}
