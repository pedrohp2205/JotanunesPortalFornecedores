using AwesomeAssertions;

using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTypeTests;

[Binding]
internal class LeituraDocumentTypeStepDefinitions(DocumentTypeApiClientFixture documentTypeFix)
{
    private List<SupplierDocumentTypeDto>? _tiposDocumento;

    [When(@"^eu listar os tipos de documento da minha empresa$")]
    public async Task QuandoEuListarOsTiposDeDocumentoDaMinhaEmpresa()
    {
        _tiposDocumento = await documentTypeFix.GetAsync();
    }

    [When(@"^eu listar os tipos de documento da minha empresa para o fornecimento de (material|mão de obra)$")]
    public async Task QuandoEuListarOsTiposDeDocumentoDaMinhaEmpresaParaOFornecimentoDe(string tipoFornecimento)
    {
        var supplierType = (int)SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento);
        _tiposDocumento = await documentTypeFix.GetAsync($"?supplierType={supplierType}");
    }

    [Then(@"^a listagem de tipos de documento deve ter (\d+) itens$")]
    public void EntaoAListagemDeTiposDeDocumentoDeveTerItens(int quantidade)
    {
        _tiposDocumento.Should().NotBeNull();
        _tiposDocumento!.Should().HaveCount(quantidade);
    }
}
