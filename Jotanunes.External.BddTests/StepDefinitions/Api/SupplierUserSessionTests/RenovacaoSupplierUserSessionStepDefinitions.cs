using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserSessionTests;

[Binding]
internal class RenovacaoSupplierUserSessionStepDefinitions(
    AuthApiClientFixture authFix,
    HttpResponseContext httpResponseCtx,
    SupplierUserGivenContext supplierUserCtx,
    TokenResultContext tokenResultCtx)
{
    [When(@"eu renovar a sessão com o refresh token atual")]
    public async Task QuandoEuRenovarASessaoComORefreshTokenAtual()
    {
        tokenResultCtx.Token = await authFix.RefreshAsync(new RefreshTokenDto { RefreshToken = supplierUserCtx.RefreshTokenAtual! });
    }

    [When(@"eu renovar a sessão com o refresh token ""(.*)""")]
    public async Task QuandoEuRenovarASessaoComORefreshToken(string refreshToken)
    {
        tokenResultCtx.Token = await authFix.RefreshAsync(new RefreshTokenDto { RefreshToken = refreshToken });
    }

    [Then(@"eu recebo um novo par de tokens com o refresh token rotacionado")]
    public void EntaoEuReceboUmNovoParDeTokensComORefreshTokenRotacionado()
    {
        tokenResultCtx.Token.Should().NotBeNull();
        tokenResultCtx.Token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResultCtx.Token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokenResultCtx.Token.RefreshToken.Should().NotBe(supplierUserCtx.RefreshTokenAtual);
    }

    [Then(@"o refresh token da sessão encerrada não deve mais renovar a sessão")]
    public async Task EntaoORefreshTokenDaSessaoEncerradaNaoDeveMaisRenovarASessao()
    {
        await authFix.RefreshAsync(new RefreshTokenDto { RefreshToken = supplierUserCtx.RefreshTokenAtual! });
        ((int)httpResponseCtx.Response!.StatusCode).Should().Be(StatusCodes.Status401Unauthorized);
    }
}
