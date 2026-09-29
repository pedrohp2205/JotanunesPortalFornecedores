using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class AtivacaoSupplierUserStepDefinitions(
    SupplierUserApiClientFixture supplierUserFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx,
    SupplierUserResultContext supplierUserResultCtx)
{
    [When(@"eu ativar o acesso cadastrado")]
    public async Task QuandoEuAtivarOAcessoCadastrado()
    {
        supplierUserResultCtx.Acesso = await supplierUserFix.ActivateAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioCadastrado!.Value);
    }

    [When(@"eu desativar o acesso cadastrado")]
    public async Task QuandoEuDesativarOAcessoCadastrado()
    {
        supplierUserResultCtx.Acesso = await supplierUserFix.DeactivateAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioCadastrado!.Value);
    }

    [When(@"^eu (ativar|desativar) o acesso da outra empresa pela empresa cadastrada$")]
    public async Task QuandoEuAlterarOAcessoDaOutraEmpresaPelaEmpresaCadastrada(string acao)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada!.Value;
        var idUsuario = supplierUserCtx.IdUsuarioOutraEmpresa!.Value;

        if (acao == "ativar")
        {
            await supplierUserFix.ActivateAsync(idEmpresa, idUsuario);
        }
        else
        {
            await supplierUserFix.DeactivateAsync(idEmpresa, idUsuario);
        }
    }

    [Then(@"^o acesso retornado deve estar (ativo|inativo)$")]
    public void EntaoOAcessoRetornadoDeveEstar(string situacao)
    {
        supplierUserResultCtx.Acesso.Should().NotBeNull();
        supplierUserResultCtx.Acesso!.Active.Should().Be(situacao == "ativo");
    }

    [Then(@"o acesso retornado deve estar ativo e exigir a troca de senha")]
    public void EntaoOAcessoRetornadoDeveEstarAtivoEExigirATrocaDeSenha()
    {
        supplierUserResultCtx.Acesso.Should().NotBeNull();
        supplierUserResultCtx.Acesso!.Active.Should().BeTrue();
        supplierUserResultCtx.Acesso.MustChangePassword.Should().BeTrue();
    }
}
