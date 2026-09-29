using AwesomeAssertions;

using Jotanunes.Application.DTOs.Documents;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class LeituraDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    CompanyGivenContext companyCtx,
    DocumentGivenContext documentCtx,
    DocumentResultContext documentResultCtx)
{
    private PageListResponseDto<DocumentDto>? _documentos;

    [When(@"eu listar os documentos da minha empresa")]
    public async Task QuandoEuListarOsDocumentosDaMinhaEmpresa()
    {
        _documentos = await documentFix.GetAsync();
    }

    [When(@"eu consultar o documento cadastrado")]
    public async Task QuandoEuConsultarODocumentoCadastrado()
    {
        documentResultCtx.Documento = await documentFix.GetByIdAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu consultar o documento da outra empresa")]
    public async Task QuandoEuConsultarODocumentoDaOutraEmpresa()
    {
        await documentFix.GetByIdAsync(documentCtx.IdDocumentoOutraEmpresa!.Value);
    }

    [Then(@"a listagem de documentos contém apenas o documento da minha empresa")]
    public void EntaoAListagemDeDocumentosContemApenasODocumentoDaMinhaEmpresa()
    {
        _documentos.Should().NotBeNull();
        _documentos!.Items.Select(d => d.Id).Should().ContainSingle()
            .Which.Should().Be(documentCtx.IdDocumentoCadastrado);
    }

    [Then(@"o documento retornado deve ser o documento cadastrado")]
    public void EntaoODocumentoRetornadoDeveSerODocumentoCadastrado()
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.Id.Should().Be(documentCtx.IdDocumentoCadastrado);
        documentResultCtx.Documento.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
    }
}
