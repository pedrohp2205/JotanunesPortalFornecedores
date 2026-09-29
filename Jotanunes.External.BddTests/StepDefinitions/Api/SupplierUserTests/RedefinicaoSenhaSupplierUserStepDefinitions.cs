using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class RedefinicaoSenhaSupplierUserStepDefinitions(
    AuthApiClientFixture authFix,
    HttpResponseContext httpResponseCtx)
{
    [Given(@"que a senha de ""(.*)"" já foi redefinida com o token ""(.*)""")]
    public async Task DadoQueASenhaJaFoiRedefinidaComOToken(string email, string token)
    {
        await RedefinirAsync(email, token, "PrimeiraSenha@2026");
        httpResponseCtx.Response!.IsSuccessStatusCode.Should().BeTrue();
    }

    [When(@"eu redefinir a senha de ""(.*)"" com o token ""(.*)"" para ""(.*)""")]
    public async Task QuandoEuRedefinirASenhaComOToken(string email, string token, string novaSenha)
    {
        await RedefinirAsync(email, token, novaSenha);
    }

    [Then(@"eu consigo entrar com o e-mail ""(.*)"" e a senha ""(.*)""")]
    public async Task EntaoEuConsigoEntrarComOEmailEASenha(string email, string senha)
    {
        await authFix.LoginAsync(new LoginDto { Email = email, Password = senha });
        ((int)httpResponseCtx.Response!.StatusCode).Should().Be(StatusCodes.Status200OK);
    }

    private async Task RedefinirAsync(string email, string token, string novaSenha)
    {
        await authFix.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = email,
            Token = token,
            NewPassword = novaSenha,
            NewPasswordConfirmation = novaSenha
        });
    }
}
