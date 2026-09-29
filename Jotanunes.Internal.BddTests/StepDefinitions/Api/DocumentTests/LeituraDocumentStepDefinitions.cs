using AwesomeAssertions;

using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class LeituraDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    CompanyGivenContext companyCtx,
    DocumentGivenContext documentCtx,
    DocumentResultContext documentResultCtx)
{
    private PageListResponseDto<DocumentDto>? _documentos;

    [When(@"eu listar os documentos da empresa cadastrada")]
    public async Task QuandoEuListarOsDocumentosDaEmpresaCadastrada()
    {
        _documentos = await documentFix.GetAsync($"?companyId={companyCtx.IdEmpresaCadastrada}");
    }

    [When(@"eu consultar o documento cadastrado")]
    public async Task QuandoEuConsultarODocumentoCadastrado()
    {
        documentResultCtx.Documento = await documentFix.GetByIdAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu consultar um documento inexistente")]
    public async Task QuandoEuConsultarUmDocumentoInexistente()
    {
        await documentFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de documentos contém o documento cadastrado")]
    public void EntaoAListagemDeDocumentosContemODocumentoCadastrado()
    {
        _documentos.Should().NotBeNull();
        _documentos!.Items.Select(d => d.Id).Should().ContainSingle()
            .Which.Should().Be(documentCtx.IdDocumentoCadastrado);
    }

    [Then(@"o documento retornado deve ser o Cartão de CNPJ da empresa cadastrada")]
    public void EntaoODocumentoRetornadoDeveSerOCartaoDeCnpjDaEmpresaCadastrada()
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.Id.Should().Be(documentCtx.IdDocumentoCadastrado);
        documentResultCtx.Documento.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        documentResultCtx.Documento.DocumentTypeCode.Should().Be("CNPJ_CARD");
    }
}
