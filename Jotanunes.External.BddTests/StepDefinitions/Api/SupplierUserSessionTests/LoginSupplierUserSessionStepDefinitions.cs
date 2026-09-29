using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserSessionTests;

[Binding]
internal class LoginSupplierUserSessionStepDefinitions(
    AuthApiClientFixture authFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx,
    TokenResultContext tokenResultCtx)
{
    [Given(@"^que o fornecedor errou a senha (\d+) vezes seguidas$")]
    public async Task DadoQueOFornecedorErrouASenhaVezesSeguidas(int tentativas)
    {
        for (var i = 0; i < tentativas; i++)
        {
            await authFix.LoginAsync(new LoginDto { Email = supplierUserCtx.EmailAtual!, Password = "senha-errada" });
        }
    }

    [When(@"eu entrar com o e-mail ""(.*)"" e a senha correta")]
    public async Task QuandoEuEntrarComOEmailEASenhaCorreta(string email)
    {
        tokenResultCtx.Token = await authFix.LoginAsync(new LoginDto { Email = email, Password = supplierUserCtx.SenhaAtual! });
    }

    [When(@"eu entrar com o e-mail ""(.*)"" e a senha ""(.*)""")]
    public async Task QuandoEuEntrarComOEmailEASenha(string email, string senha)
    {
        tokenResultCtx.Token = await authFix.LoginAsync(new LoginDto { Email = email, Password = senha });
    }

    [Then(@"eu recebo um par de tokens vinculado à empresa cadastrada")]
    public void EntaoEuReceboUmParDeTokensVinculadoAEmpresaCadastrada()
    {
        tokenResultCtx.Token.Should().NotBeNull();
        tokenResultCtx.Token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResultCtx.Token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokenResultCtx.Token.User.Email.Should().Be(supplierUserCtx.EmailAtual);
        tokenResultCtx.Token.User.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        tokenResultCtx.Token.User.MustChangePassword.Should().BeFalse();
    }

    [Then(@"o usuário do token deve precisar trocar a senha")]
    public void EntaoOUsuarioDoTokenDevePrecisarTrocarASenha()
    {
        tokenResultCtx.Token.Should().NotBeNull();
        tokenResultCtx.Token!.User.MustChangePassword.Should().BeTrue();
    }
}
