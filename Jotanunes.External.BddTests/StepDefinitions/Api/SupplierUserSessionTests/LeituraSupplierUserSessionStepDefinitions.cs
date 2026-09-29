using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserSessionTests;

[Binding]
internal class LeituraSupplierUserSessionStepDefinitions(
    AuthApiClientFixture authFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx)
{
    private AuthenticatedUserDto? _usuario;

    [When(@"eu solicitar os dados do usuário autenticado")]
    public async Task QuandoEuSolicitarOsDadosDoUsuarioAutenticado()
    {
        _usuario = await authFix.GetMeAsync();
    }

    [Then(@"os dados devem refletir o fornecedor autenticado")]
    public void EntaoOsDadosDevemRefletirOFornecedorAutenticado()
    {
        _usuario.Should().NotBeNull();
        _usuario!.Id.Should().Be(supplierUserCtx.IdUsuarioCadastrado);
        _usuario.Email.Should().Be(supplierUserCtx.EmailAtual);
        _usuario.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        _usuario.CompanyCnpj.Should().Be(companyCtx.CnpjEmpresaCadastrada);
        _usuario.MustChangePassword.Should().BeFalse();
    }
}
