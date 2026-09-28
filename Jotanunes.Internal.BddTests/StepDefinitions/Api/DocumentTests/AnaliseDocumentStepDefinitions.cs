using AwesomeAssertions;

using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Entities;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class AnaliseDocumentStepDefinitions(
    DatabaseFixture databaseFixture,
    DocumentApiClientFixture documentFix,
    DocumentGivenContext documentCtx)
{
    private DocumentAnalysisDto? _analise;

    [Given(@"^que o documento cadastrado tem uma análise (pendente|concluída)$")]
    public async Task DadoQueODocumentoCadastradoTemUmaAnalise(string situacao)
    {
        await using var context = databaseFixture.CreateDbContext();

        var documento = await context.Documents.FindAsync(documentCtx.IdDocumentoCadastrado!.Value);
        var analise = new DocumentAnalysis(documento!);
        if (situacao == "concluída")
        {
            analise.MarkNotSupported();
        }

        context.DocumentAnalyses.Add(analise);
        await context.SaveChangesAsync();
    }

    [When(@"eu consultar a análise do documento cadastrado")]
    public async Task QuandoEuConsultarAAnaliseDoDocumentoCadastrado()
    {
        _analise = await documentFix.GetAnalysisAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu pedir uma nova análise do documento cadastrado")]
    public async Task QuandoEuPedirUmaNovaAnaliseDoDocumentoCadastrado()
    {
        _analise = await documentFix.ReanalyzeAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu pedir uma nova análise de um documento inexistente")]
    public async Task QuandoEuPedirUmaNovaAnaliseDeUmDocumentoInexistente()
    {
        await documentFix.ReanalyzeAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a análise retornada deve estar com status ""(.*)""")]
    public void EntaoAAnaliseRetornadaDeveEstarComStatus(string status)
    {
        _analise.Should().NotBeNull();
        _analise!.StatusDescription.Should().Be(status);
        _analise.DocumentId.Should().Be(documentCtx.IdDocumentoCadastrado!.Value);
    }
}
