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
    private PageListResponseDto<InternalDocumentDto>? _documentos;

    [When(@"eu listar os documentos da empresa cadastrada")]
    public async Task QuandoEuListarOsDocumentosDaEmpresaCadastrada()
    {
        _documentos = await documentFix.GetAsync($"?companyId={companyCtx.IdEmpresaCadastrada}");
    }

    [When(@"eu listar os documentos da empresa cadastrada com veredito da análise (\d+)")]
    public async Task QuandoEuListarOsDocumentosDaEmpresaCadastradaComVereditoDaAnalise(int veredito)
    {
        _documentos = await documentFix.GetAsync($"?companyId={companyCtx.IdEmpresaCadastrada}&analysisVerdict={veredito}");
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

    [Then(@"a listagem de documentos está vazia")]
    public void EntaoAListagemDeDocumentosEstaVazia()
    {
        _documentos.Should().NotBeNull();
        _documentos!.Items.Should().BeEmpty();
    }

    [Then(@"o documento listado mostra a análise com veredito ""(.*)"" e (\d+) apontamento\(s\) bloqueante\(s\)")]
    public void EntaoODocumentoListadoMostraAAnalise(string veredito, int bloqueantes)
    {
        _documentos.Should().NotBeNull();
        var documento = _documentos!.Items.Should().ContainSingle().Which;
        documento.Analysis.Should().NotBeNull();
        documento.Analysis!.StatusDescription.Should().Be("Completed");
        documento.Analysis.VerdictDescription.Should().Be(veredito);
        documento.Analysis.BlockingCount.Should().Be(bloqueantes);
    }

    [Then(@"o documento retornado não tem resumo da análise")]
    public void EntaoODocumentoRetornadoNaoTemResumoDaAnalise()
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.Analysis.Should().BeNull();
    }
}
