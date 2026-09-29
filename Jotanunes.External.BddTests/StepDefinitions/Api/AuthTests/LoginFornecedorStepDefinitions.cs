using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.AuthTests;

[Binding]
internal class LoginFornecedorStepDefinitions(
    AuthApiClientFixture authFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx)
{
    private TokenDto? _token;

    [When(@"eu entrar com o e-mail ""(.*)"" e a senha correta")]
    public async Task QuandoEuEntrarComOEmailEASenhaCorreta(string email)
    {
        _token = await authFix.LoginAsync(new LoginDto { Email = email, Password = supplierUserCtx.SenhaAtual! });
    }

    [When(@"eu entrar com o e-mail ""(.*)"" e a senha ""(.*)""")]
    public async Task QuandoEuEntrarComOEmailEASenha(string email, string senha)
    {
        _token = await authFix.LoginAsync(new LoginDto { Email = email, Password = senha });
    }

    [Then(@"eu recebo um par de tokens vinculado à empresa cadastrada")]
    public void EntaoEuReceboUmParDeTokensVinculadoAEmpresaCadastrada()
    {
        _token.Should().NotBeNull();
        _token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        _token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        _token.User.Email.Should().Be(supplierUserCtx.EmailAtual);
        _token.User.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
    }
}
