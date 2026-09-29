using AwesomeAssertions;

using Jotanunes.Domain.Enums;
using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class EnvioDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    DocumentResultContext documentResultCtx)
{
    [When(@"eu enviar o Cartão de CNPJ em PDF")]
    public async Task QuandoEuEnviarOCartaoDeCnpjEmPdf()
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ, DocumentUploadDriver.ArquivoPdf));
    }

    [When(@"^eu enviar o Cartão de CNPJ com (um arquivo de texto|um arquivo vazio)$")]
    public async Task QuandoEuEnviarOCartaoDeCnpjComArquivoInvalido(string arquivo)
    {
        var (conteudo, nomeArquivo) = arquivo == "um arquivo vazio"
            ? (Array.Empty<byte>(), "vazio.pdf")
            : (DocumentUploadDriver.ArquivoTexto, "documento.txt");

        await EnviarAsync(DocumentUploadDriver.CriarFormulario(TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ, conteudo, nomeArquivo));
    }

    [When(@"eu enviar a Folha de Pagamento para a solicitação cadastrada")]
    public async Task QuandoEuEnviarAFolhaDePagamentoParaASolicitacaoCadastrada()
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(
            TestConstants.TIPO_DOCUMENTO_FOLHA_PAGAMENTO,
            DocumentUploadDriver.ArquivoPdf,
            supplyRequestId: supplyRequestCtx.IdSolicitacaoCadastrada!.Value));
    }

    [When(@"eu enviar a Folha de Pagamento para a solicitação da outra empresa")]
    public async Task QuandoEuEnviarAFolhaDePagamentoParaASolicitacaoDaOutraEmpresa()
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(
            TestConstants.TIPO_DOCUMENTO_FOLHA_PAGAMENTO,
            DocumentUploadDriver.ArquivoPdf,
            supplyRequestId: supplyRequestCtx.IdSolicitacaoOutraEmpresa!.Value));
    }

    [When(@"eu enviar a Folha de Ponto do trabalhador ""(.*)"" com CPF ""(.*)"" para a solicitação cadastrada")]
    public async Task QuandoEuEnviarAFolhaDePontoDoTrabalhador(string nome, string cpf)
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(
            TestConstants.TIPO_DOCUMENTO_FOLHA_PONTO,
            DocumentUploadDriver.ArquivoPdf,
            supplyRequestId: supplyRequestCtx.IdSolicitacaoCadastrada!.Value,
            workerName: nome,
            workerCpf: cpf));
    }

    [When(@"eu enviar a Folha de Ponto sem informar o trabalhador para a solicitação cadastrada")]
    public async Task QuandoEuEnviarAFolhaDePontoSemInformarOTrabalhador()
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(
            TestConstants.TIPO_DOCUMENTO_FOLHA_PONTO,
            DocumentUploadDriver.ArquivoPdf,
            supplyRequestId: supplyRequestCtx.IdSolicitacaoCadastrada!.Value));
    }

    [When(@"eu enviar um documento de um tipo inexistente")]
    public async Task QuandoEuEnviarUmDocumentoDeUmTipoInexistente()
    {
        await EnviarAsync(DocumentUploadDriver.CriarFormulario(TestConstants.ID_INEXISTENTE, DocumentUploadDriver.ArquivoPdf));
    }

    [Then(@"o documento enviado deve estar pendente de avaliação sem vínculo com solicitação")]
    public void EntaoODocumentoEnviadoDeveEstarPendenteDeAvaliacaoSemVinculoComSolicitacao()
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        documentResultCtx.Documento.DocumentTypeId.Should().Be(TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ);
        documentResultCtx.Documento.Status.Should().Be(DocumentStatus.Pending);
        documentResultCtx.Documento.ContentType.Should().Be("application/pdf");
        documentResultCtx.Documento.SupplyRequestId.Should().BeNull();
    }

    [Then(@"o documento enviado deve estar vinculado à solicitação cadastrada no período de referência atual")]
    public void EntaoODocumentoEnviadoDeveEstarVinculadoASolicitacaoCadastradaNoPeriodoDeReferenciaAtual()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.SupplyRequestId.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
        documentResultCtx.Documento.ReferencePeriodStart.Should().Be(new DateOnly(hoje.Year, hoje.Month, 1));
        documentResultCtx.Documento.ReferencePeriodEnd.Should().Be(new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month)));
    }

    [Then(@"o documento enviado deve ser do trabalhador com CPF ""(.*)""")]
    public void EntaoODocumentoEnviadoDeveSerDoTrabalhadorComCpf(string cpf)
    {
        documentResultCtx.Documento.Should().NotBeNull();
        documentResultCtx.Documento!.WorkerCpf.Should().Be(cpf);
    }

    [Then(@"a solicitação cadastrada deve estar em andamento")]
    public async Task EntaoASolicitacaoCadastradaDeveEstarEmAndamento()
    {
        await using var context = databaseFixture.CreateDbContext();
        var solicitacao = await context.SupplyRequests.AsNoTracking().SingleAsync(s => s.Id == supplyRequestCtx.IdSolicitacaoCadastrada);
        solicitacao.Status.Should().Be(SupplyRequestStatus.InProgress);
    }

    private async Task EnviarAsync(MultipartFormDataContent formulario)
    {
        using (formulario)
        {
            documentResultCtx.Documento = await documentFix.UploadAsync(formulario);
        }
    }
}
