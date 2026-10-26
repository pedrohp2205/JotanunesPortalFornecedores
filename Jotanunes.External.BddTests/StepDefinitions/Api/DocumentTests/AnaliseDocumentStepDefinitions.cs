using System.Text.Json;

using AwesomeAssertions;

using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class AnaliseDocumentStepDefinitions(
    DatabaseFixture databaseFixture,
    DocumentApiClientFixture documentFix,
    DocumentGivenContext documentCtx,
    HttpResponseContext httpResponseCtx)
{
    [Given(@"^que o documento cadastrado tem uma análise não conforme$")]
    public async Task DadoQueODocumentoCadastradoTemUmaAnaliseNaoConforme()
    {
        await using var context = databaseFixture.CreateDbContext();

        var documento = await context.Documents.FindAsync(documentCtx.IdDocumentoCadastrado!.Value);
        var analise = new DocumentAnalysis(documento!);
        analise.Complete(TextExtractionEngine.NativeText, [], [
            new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "CRF vencida em 09/08/2026.")
        ]);

        context.DocumentAnalyses.Add(analise);
        await context.SaveChangesAsync();
    }

    [Given(@"^que a análise do documento cadastrado identificou outro documento$")]
    public async Task DadoQueAAnaliseDoDocumentoCadastradoIdentificouOutroDocumento()
    {
        await using var context = databaseFixture.CreateDbContext();

        var documento = await context.Documents.FindAsync(documentCtx.IdDocumentoCadastrado!.Value);
        var analise = new DocumentAnalysis(documento!);
        analise.Complete(TextExtractionEngine.Vision, [], [
            new AnalysisFinding(DocumentAnalysis.WrongDocumentTypeCode, FindingSeverity.Blocking, "O arquivo parece ser CNH, e não Cartão de CNPJ.")
        ]);

        context.DocumentAnalyses.Add(analise);
        await context.SaveChangesAsync();
    }

    [When(@"eu listar os documentos da minha empresa filtrando pelo veredito da análise")]
    public async Task QuandoEuListarOsDocumentosDaMinhaEmpresaFiltrandoPeloVereditoDaAnalise()
    {
        await documentFix.GetAsync("?analysisVerdict=1");
    }

    [Then(@"a resposta não contém o parecer da análise automática")]
    public async Task EntaoARespostaNaoContemOParecerDaAnaliseAutomatica()
    {
        httpResponseCtx.Response.Should().NotBeNull();
        var corpo = await httpResponseCtx.Response!.Content.ReadAsStringAsync();

        corpo.Should().NotContainEquivalentOf("\"analysis\"");
        corpo.Should().NotContain("NonConforming");
        corpo.Should().NotContain("CRF vencida");
    }

    [Then(@"a listagem ainda contém o documento cadastrado")]
    public async Task EntaoAListagemAindaContemODocumentoCadastrado()
    {
        httpResponseCtx.Response.Should().NotBeNull();
        using var corpo = JsonDocument.Parse(await httpResponseCtx.Response!.Content.ReadAsStringAsync());

        var ids = corpo.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64());
        ids.Should().Contain(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [Then(@"o documento retornado avisa que o arquivo pode não ser o documento certo")]
    public async Task EntaoODocumentoRetornadoAvisaQueOArquivoPodeNaoSerODocumentoCerto()
    {
        httpResponseCtx.Response.Should().NotBeNull();
        using var corpo = JsonDocument.Parse(await httpResponseCtx.Response!.Content.ReadAsStringAsync());

        corpo.RootElement.GetProperty("uploadWarning").GetString().Should()
            .Be("Não conseguimos identificar este arquivo como \"Cartão de CNPJ\". Confira se enviou o documento certo; se não for, envie o documento correto.");
    }
}
