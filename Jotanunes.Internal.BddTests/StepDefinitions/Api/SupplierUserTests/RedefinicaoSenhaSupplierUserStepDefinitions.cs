using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class RedefinicaoSenhaSupplierUserStepDefinitions(
    SupplierUserApiClientFixture supplierUserFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx,
    SupplierUserResultContext supplierUserResultCtx)
{
    [When(@"eu redefinir a senha do acesso cadastrado")]
    public async Task QuandoEuRedefinirASenhaDoAcessoCadastrado()
    {
        supplierUserResultCtx.Acesso = await supplierUserFix.ResetPasswordAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioCadastrado!.Value);
    }

    [When(@"eu redefinir a senha do acesso da outra empresa pela empresa cadastrada")]
    public async Task QuandoEuRedefinirASenhaDoAcessoDaOutraEmpresaPelaEmpresaCadastrada()
    {
        await supplierUserFix.ResetPasswordAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioOutraEmpresa!.Value);
    }
}
