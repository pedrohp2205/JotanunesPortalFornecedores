using AwesomeAssertions;

using Jotanunes.Application.DTOs.Users;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class LeituraSupplierUserStepDefinitions(
    SupplierUserApiClientFixture supplierUserFix,
    CompanyGivenContext companyCtx)
{
    private List<SupplierUserDto>? _acessos;

    [When(@"eu listar os acessos da empresa cadastrada")]
    public async Task QuandoEuListarOsAcessosDaEmpresaCadastrada()
    {
        _acessos = await supplierUserFix.GetByCompanyAsync(companyCtx.IdEmpresaCadastrada!.Value);
    }

    [When(@"eu listar os acessos de uma empresa inexistente")]
    public async Task QuandoEuListarOsAcessosDeUmaEmpresaInexistente()
    {
        await supplierUserFix.GetByCompanyAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de acessos contém o usuário ""(.*)""")]
    public void EntaoAListagemDeAcessosContemOUsuario(string email)
    {
        _acessos.Should().NotBeNull();
        _acessos!.Select(a => a.Email).Should().Contain(email);
    }
}
