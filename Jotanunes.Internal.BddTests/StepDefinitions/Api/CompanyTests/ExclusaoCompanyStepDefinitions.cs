using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class ExclusaoCompanyStepDefinitions(
    CompanyApiClientFixture companyFix,
    CompanyGivenContext companyCtx,
    HttpResponseContext httpResponseCtx)
{
    [When(@"eu excluir a empresa cadastrada")]
    public async Task QuandoEuExcluirAEmpresaCadastrada()
    {
        await companyFix.DeleteAsync(companyCtx.IdEmpresaCadastrada!.Value);
    }

    [When(@"eu excluir uma empresa inexistente")]
    public async Task QuandoEuExcluirUmaEmpresaInexistente()
    {
        await companyFix.DeleteAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a empresa cadastrada não deve mais ser encontrada")]
    public async Task EntaoAEmpresaCadastradaNaoDeveMaisSerEncontrada()
    {
        await companyFix.GetByIdAsync(companyCtx.IdEmpresaCadastrada!.Value);
        ((int)httpResponseCtx.Response!.StatusCode).Should().Be(StatusCodes.Status404NotFound);
    }
}
