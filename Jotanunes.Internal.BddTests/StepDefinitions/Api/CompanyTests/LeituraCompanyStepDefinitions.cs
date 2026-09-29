using AwesomeAssertions;

using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class LeituraCompanyStepDefinitions(
    CompanyApiClientFixture companyFix,
    CompanyGivenContext companyCtx)
{
    private PageListResponseDto<CompanyDto>? _empresas;
    private CompanyDto? _empresa;

    [When(@"eu listar as empresas filtrando pelo CNPJ ""(.*)""")]
    public async Task QuandoEuListarAsEmpresasFiltrandoPeloCnpj(string cnpj)
    {
        _empresas = await companyFix.GetAsync($"?cnpj={Uri.EscapeDataString(cnpj)}");
    }

    [When(@"eu consultar a empresa cadastrada")]
    public async Task QuandoEuConsultarAEmpresaCadastrada()
    {
        _empresa = await companyFix.GetByIdAsync(companyCtx.IdEmpresaCadastrada!.Value);
    }

    [When(@"eu consultar uma empresa inexistente")]
    public async Task QuandoEuConsultarUmaEmpresaInexistente()
    {
        await companyFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de empresas contém apenas a empresa cadastrada")]
    public void EntaoAListagemDeEmpresasContemApenasAEmpresaCadastrada()
    {
        _empresas.Should().NotBeNull();
        _empresas!.Items.Select(e => e.Id).Should().ContainSingle()
            .Which.Should().Be(companyCtx.IdEmpresaCadastrada);
    }

    [Then(@"os dados retornados devem ser da empresa cadastrada")]
    public void EntaoOsDadosRetornadosDevemSerDaEmpresaCadastrada()
    {
        _empresa.Should().NotBeNull();
        _empresa!.Id.Should().Be(companyCtx.IdEmpresaCadastrada);
        _empresa.Cnpj.Should().Be(companyCtx.CnpjEmpresaCadastrada);
    }
}
