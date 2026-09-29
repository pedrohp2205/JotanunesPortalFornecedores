using AwesomeAssertions;

using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class AvaliacaoDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    DocumentGivenContext documentCtx,
    DocumentResultContext documentResultCtx)
{
    [When(@"eu aprovar o documento cadastrado")]
    public async Task QuandoEuAprovarODocumentoCadastrado()
    {
        documentResultCtx.Documento = await documentFix.ApproveAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu rejeitar o documento cadastrado pelo motivo ""(.*)""")]
    public async Task QuandoEuRejeitarODocumentoCadastradoPeloMotivo(string motivo)
    {
        documentResultCtx.Documento = await documentFix.RejectAsync(
            documentCtx.IdDocumentoCadastrado!.Value,
            new DocumentRejectDto { Reason = motivo });
    }

    [When(@"eu aprovar um documento inexistente")]
    public async Task QuandoEuAprovarUmDocumentoInexistente()
    {
        await documentFix.ApproveAsync(TestConstants.ID_INEXISTENTE);
    }

    [When(@"eu rejeitar um documento inexistente")]
    public async Task QuandoEuRejeitarUmDocumentoInexistente()
    {
        await documentFix.RejectAsync(TestConstants.ID_INEXISTENTE, new DocumentRejectDto { Reason = "Documento ilegível" });
    }

    [Then(@"o documento retornado deve estar aprovado")]
    public void EntaoODocumentoRetornadoDeveEstarAprovado()
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.Status.Should().Be(DocumentStatus.Approved);
        documentResultCtx.Documento.ReviewedAt.Should().NotBeNull();
    }

    [Then(@"o documento retornado deve estar rejeitado pelo motivo ""(.*)""")]
    public void EntaoODocumentoRetornadoDeveEstarRejeitadoPeloMotivo(string motivo)
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.Status.Should().Be(DocumentStatus.Rejected);
        documentResultCtx.Documento.RejectionReason.Should().Be(motivo);
    }

    [Then(@"a empresa cadastrada deve estar apta")]
    public async Task EntaoAEmpresaCadastradaDeveEstarApta()
    {
        (await ObterStatusEmpresaCadastradaAsync()).Should().Be(CompanyStatus.Eligible);
    }

    [Then(@"a empresa cadastrada deve continuar aguardando documentação")]
    public async Task EntaoAEmpresaCadastradaDeveContinuarAguardandoDocumentacao()
    {
        (await ObterStatusEmpresaCadastradaAsync()).Should().Be(CompanyStatus.PendingDocumentation);
    }

    private async Task<CompanyStatus> ObterStatusEmpresaCadastradaAsync()
    {
        await using var context = databaseFixture.CreateDbContext();
        var empresa = await context.Companies.AsNoTracking().SingleAsync(c => c.Id == companyCtx.IdEmpresaCadastrada);
        return empresa.Status;
    }
}
