using AwesomeAssertions;

using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTypeTests;

[Binding]
internal class LeituraDocumentTypeStepDefinitions(
    DocumentTypeApiClientFixture documentTypeFix,
    DocumentTypeResultContext documentTypeResultCtx)
{
    private List<DocumentTypeDto>? _tiposDocumento;

    [When(@"eu listar os tipos de documento")]
    public async Task QuandoEuListarOsTiposDeDocumento()
    {
        _tiposDocumento = await documentTypeFix.GetAsync();
    }

    [When(@"eu consultar o tipo de documento Cartão de CNPJ")]
    public async Task QuandoEuConsultarOTipoDeDocumentoCartaoDeCnpj()
    {
        documentTypeResultCtx.TipoDocumento = await documentTypeFix.GetByIdAsync(TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ);
    }

    [When(@"eu consultar um tipo de documento inexistente")]
    public async Task QuandoEuConsultarUmTipoDeDocumentoInexistente()
    {
        await documentTypeFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de tipos de documento contém os tipos padrão do sistema")]
    public void EntaoAListagemDeTiposDeDocumentoContemOsTiposPadraoDoSistema()
    {
        _tiposDocumento.Should().NotBeNull();
        _tiposDocumento!.Should().HaveCountGreaterThanOrEqualTo(TestConstants.TOTAL_TIPOS_DOCUMENTO_CADASTRADOS);
        _tiposDocumento.Select(t => t.Code).Should().Contain(["CNPJ_CARD", "PAYROLL", "TIMESHEET"]);
    }

    [Then(@"o tipo de documento retornado deve ter o código ""(.*)"" e estar ativo")]
    public void EntaoOTipoDeDocumentoRetornadoDeveTerOCodigoEEstarAtivo(string codigo)
    {
        documentTypeResultCtx.TipoDocumento.Should().NotBeNull();
        documentTypeResultCtx.TipoDocumento!.Code.Should().Be(codigo);
        documentTypeResultCtx.TipoDocumento.Active.Should().BeTrue();
    }
}
