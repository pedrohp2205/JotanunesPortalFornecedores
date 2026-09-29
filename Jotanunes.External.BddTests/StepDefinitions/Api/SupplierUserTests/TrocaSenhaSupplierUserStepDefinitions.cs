using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class TrocaSenhaSupplierUserStepDefinitions(
    AuthApiClientFixture authFix,
    ApiClientFixture apiClientFix,
    SupplyRequestApiClientFixture supplyRequestFix,
    HttpResponseContext httpResponseCtx,
    SupplierUserGivenContext supplierUserCtx,
    TokenResultContext tokenResultCtx)
{
    [When(@"eu trocar a minha senha para ""(.*)""")]
    public async Task QuandoEuTrocarAMinhaSenhaPara(string novaSenha)
    {
        await TrocarSenhaAsync(supplierUserCtx.SenhaAtual!, novaSenha);
    }

    [When(@"eu trocar a minha senha informando a senha atual ""(.*)""")]
    public async Task QuandoEuTrocarAMinhaSenhaInformandoASenhaAtual(string senhaAtual)
    {
        await TrocarSenhaAsync(senhaAtual, "NovaSenha@2026");
    }

    [When(@"eu trocar a minha senha para a senha atual")]
    public async Task QuandoEuTrocarAMinhaSenhaParaASenhaAtual()
    {
        await TrocarSenhaAsync(supplierUserCtx.SenhaAtual!, supplierUserCtx.SenhaAtual!);
    }

    [Then(@"o usuário do novo token não deve mais precisar trocar a senha")]
    public void EntaoOUsuarioDoNovoTokenNaoDeveMaisPrecisarTrocarASenha()
    {
        tokenResultCtx.Token.Should().NotBeNull();
        tokenResultCtx.Token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResultCtx.Token.User.MustChangePassword.Should().BeFalse();
    }

    [Then(@"com o novo token eu consigo listar as solicitações da minha empresa")]
    public async Task EntaoComONovoTokenEuConsigoListarAsSolicitacoesDaMinhaEmpresa()
    {
        apiClientFix.UseAccessToken(tokenResultCtx.Token!.AccessToken);
        await supplyRequestFix.GetAsync();
        ((int)httpResponseCtx.Response!.StatusCode).Should().Be(StatusCodes.Status200OK);
    }

    private async Task TrocarSenhaAsync(string senhaAtual, string novaSenha)
    {
        tokenResultCtx.Token = await authFix.ChangePasswordAsync(new ChangePasswordDto
        {
            CurrentPassword = senhaAtual,
            NewPassword = novaSenha,
            NewPasswordConfirmation = novaSenha
        });
    }
}
