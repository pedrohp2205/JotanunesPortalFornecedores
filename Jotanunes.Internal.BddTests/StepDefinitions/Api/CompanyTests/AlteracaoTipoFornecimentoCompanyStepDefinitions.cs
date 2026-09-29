using AwesomeAssertions;

using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class AlteracaoTipoFornecimentoCompanyStepDefinitions(
    CompanyApiClientFixture companyFix,
    CompanyGivenContext companyCtx,
    CompanyResultContext companyResultCtx)
{
    [When(@"^eu alterar o tipo de fornecimento da empresa cadastrada para (material|mão de obra|material e mão de obra)$")]
    public async Task QuandoEuAlterarOTipoDeFornecimentoDaEmpresaCadastrada(string tipoFornecimento)
    {
        companyResultCtx.Empresa = await companyFix.ChangeSupplierTypeAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            new CompanyChangeSupplierTypeDto { SupplierType = SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento) });
    }

    [When(@"eu alterar o tipo de fornecimento de uma empresa inexistente")]
    public async Task QuandoEuAlterarOTipoDeFornecimentoDeUmaEmpresaInexistente()
    {
        await companyFix.ChangeSupplierTypeAsync(
            TestConstants.ID_INEXISTENTE,
            new CompanyChangeSupplierTypeDto { SupplierType = SupplierType.Material });
    }

    [Then(@"^a empresa retornada deve fornecer (material|mão de obra|material e mão de obra)$")]
    public void EntaoAEmpresaRetornadaDeveFornecer(string tipoFornecimento)
    {
        companyResultCtx.Empresa.Should().NotBeNull();
        companyResultCtx.Empresa!.SupplierType.Should().Be(SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento));
    }
}
