using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class CadastroSupplierUserStepDefinitions(
    SupplierUserApiClientFixture supplierUserFix,
    CompanyGivenContext companyCtx,
    SupplierUserResultContext supplierUserResultCtx)
{
    [When(@"eu criar o acesso ""(.*)"" com a senha provisória ""(.*)"" para a empresa cadastrada")]
    public async Task QuandoEuCriarOAcessoParaAEmpresaCadastrada(string email, string senhaProvisoria)
    {
        supplierUserResultCtx.Acesso = await supplierUserFix.CreateAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            SupplierUserDomainDriver.CriarSupplierUserCreateDtoValido(email, senhaProvisoria));
    }

    [When(@"eu criar o acesso ""(.*)"" com a senha provisória ""(.*)"" para uma empresa inexistente")]
    public async Task QuandoEuCriarOAcessoParaUmaEmpresaInexistente(string email, string senhaProvisoria)
    {
        await supplierUserFix.CreateAsync(
            TestConstants.ID_INEXISTENTE,
            SupplierUserDomainDriver.CriarSupplierUserCreateDtoValido(email, senhaProvisoria));
    }
}
