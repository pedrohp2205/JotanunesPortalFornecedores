using AwesomeAssertions;

using Jotanunes.Domain.Entities;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.WorkerTests;

[Binding]
internal class AlocacaoWorkerStepDefinitions(
    WorkerApiClientFixture workerFix,
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    WorkerGivenContext workerCtx,
    WorkerResultContext workerResultCtx)
{
    private static readonly string[] CpfsDeOutrosTrabalhadores = ["123.456.789-09", "987.654.321-00", "246.813.579-28", "135.792.468-28"];

    [Given(@"^que a solicitação cadastrada já tem todos os trabalhadores necessários alocados$")]
    public async Task DadoQueASolicitacaoCadastradaJaTemTodosOsTrabalhadoresNecessariosAlocados()
    {
        await using var context = databaseFixture.CreateDbContext();

        var solicitacao = await context.SupplyRequests.SingleAsync(s => s.Id == supplyRequestCtx.IdSolicitacaoCadastrada);
        var necessarios = solicitacao.RequiredWorkerCount
            ?? throw new InvalidOperationException("A solicitação cadastrada não define a quantidade de trabalhadores necessária.");

        for (var i = 0; i < necessarios; i++)
        {
            var trabalhador = new Worker(companyCtx.IdEmpresaCadastrada!.Value, $"Trabalhador {i + 1}", CpfsDeOutrosTrabalhadores[i]);
            context.Workers.Add(trabalhador);
            context.WorkerAllocations.Add(new WorkerAllocation(solicitacao, trabalhador, i));
        }

        await context.SaveChangesAsync();
    }

    [When(@"^eu alocar o trabalhador cadastrado na solicitação cadastrada$")]
    public async Task QuandoEuAlocarOTrabalhadorCadastradoNaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacao = await workerFix.AllocateAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [When(@"^eu alocar o trabalhador da outra empresa na solicitação cadastrada$")]
    public async Task QuandoEuAlocarOTrabalhadorDaOutraEmpresaNaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacao = await workerFix.AllocateAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, workerCtx.IdTrabalhadorOutraEmpresa!.Value);
    }

    [When(@"^eu desalocar o trabalhador cadastrado da solicitação cadastrada$")]
    public async Task QuandoEuDesalocarOTrabalhadorCadastradoDaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacao = await workerFix.ReleaseAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [When(@"^eu listar os trabalhadores alocados na solicitação cadastrada$")]
    public async Task QuandoEuListarOsTrabalhadoresAlocadosNaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacoes = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [Then(@"^a solicitação cadastrada deve ter apenas o trabalhador cadastrado alocado$")]
    public async Task EntaoASolicitacaoCadastradaDeveTerApenasOTrabalhadorCadastradoAlocado()
    {
        var alocacoes = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);

        alocacoes.Should().NotBeNull();
        alocacoes!.Select(a => a.WorkerId).Should().Equal(workerCtx.IdTrabalhadorCadastrado!.Value);
        alocacoes.Single().ReleasedAt.Should().BeNull();
    }

    [Then(@"^a solicitação cadastrada não deve ter trabalhadores alocados$")]
    public async Task EntaoASolicitacaoCadastradaNaoDeveTerTrabalhadoresAlocados()
    {
        var alocacoes = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);

        alocacoes.Should().NotBeNull().And.BeEmpty();
    }

    [Then(@"^o histórico da solicitação cadastrada deve mostrar o trabalhador cadastrado desalocado$")]
    public async Task EntaoOHistoricoDaSolicitacaoCadastradaDeveMostrarOTrabalhadorCadastradoDesalocado()
    {
        var historico = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, includeReleased: true);

        historico.Should().NotBeNull();
        var alocacao = historico!.Should().ContainSingle().Subject;
        alocacao.WorkerId.Should().Be(workerCtx.IdTrabalhadorCadastrado);
        alocacao.ReleasedAt.Should().NotBeNull();
    }
}
