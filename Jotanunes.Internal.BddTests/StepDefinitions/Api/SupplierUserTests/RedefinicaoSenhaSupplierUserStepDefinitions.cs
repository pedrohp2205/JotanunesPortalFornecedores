using Jotanunes.Application.DTOs.Users;
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
    [When(@"eu redefinir a senha do acesso cadastrado para ""(.*)""")]
    public async Task QuandoEuRedefinirASenhaDoAcessoCadastrado(string senhaProvisoria)
    {
        supplierUserResultCtx.Acesso = await supplierUserFix.ResetPasswordAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioCadastrado!.Value,
            new SupplierUserResetPasswordDto { TemporaryPassword = senhaProvisoria });
    }

    [When(@"eu redefinir a senha do acesso da outra empresa pela empresa cadastrada")]
    public async Task QuandoEuRedefinirASenhaDoAcessoDaOutraEmpresaPelaEmpresaCadastrada()
    {
        await supplierUserFix.ResetPasswordAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            supplierUserCtx.IdUsuarioOutraEmpresa!.Value,
            new SupplierUserResetPasswordDto { TemporaryPassword = "Provisoria@2" });
    }
}
