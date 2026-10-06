using AwesomeAssertions;

using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class CruzamentoSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    SupplyRequestGivenContext supplyRequestCtx)
{
    private PeriodComplianceReportDto? _relatorio;
    private List<PeriodComplianceReportDto>? _relatorios;

    private static PeriodComplianceRecalculateDto Periodo(string inicio, string fim) => new()
    {
        PeriodStart = DateOnly.Parse(inicio),
        PeriodEnd = DateOnly.Parse(fim)
    };

    [Given(@"que foi pedido o cruzamento da solicitação cadastrada para o período de ""(.*)"" a ""(.*)""")]
    [When(@"eu pedir o cruzamento da solicitação cadastrada para o período de ""(.*)"" a ""(.*)""")]
    public async Task QuandoEuPedirOCruzamentoDaSolicitacaoCadastrada(string inicio, string fim)
    {
        _relatorio = await supplyRequestFix.RecalculateComplianceReportAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, Periodo(inicio, fim));
    }

    [When(@"eu pedir o cruzamento de uma solicitação inexistente")]
    public async Task QuandoEuPedirOCruzamentoDeUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.RecalculateComplianceReportAsync(TestConstants.ID_INEXISTENTE, Periodo("2026-07-01", "2026-07-31"));
    }

    [When(@"eu listar os cruzamentos da solicitação cadastrada")]
    public async Task QuandoEuListarOsCruzamentosDaSolicitacaoCadastrada()
    {
        _relatorios = await supplyRequestFix.GetComplianceReportsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu listar os cruzamentos de uma solicitação inexistente")]
    public async Task QuandoEuListarOsCruzamentosDeUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.GetComplianceReportsAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"o relatório de cruzamento retornado está na fila para o período de ""(.*)"" a ""(.*)""")]
    public void EntaoORelatorioDeCruzamentoRetornadoEstaNaFila(string inicio, string fim)
    {
        _relatorio.Should().NotBeNull();
        _relatorio!.Status.Should().Be(PeriodComplianceStatus.Pending);
        _relatorio.SupplyRequestId.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
        _relatorio.PeriodStart.Should().Be(DateOnly.Parse(inicio));
        _relatorio.PeriodEnd.Should().Be(DateOnly.Parse(fim));
    }

    [Then(@"a listagem de cruzamentos tem (\d+) relatório\(s\)")]
    public void EntaoAListagemDeCruzamentosTem(int quantidade)
    {
        _relatorios.Should().NotBeNull();
        _relatorios!.Should().HaveCount(quantidade);
    }
}
