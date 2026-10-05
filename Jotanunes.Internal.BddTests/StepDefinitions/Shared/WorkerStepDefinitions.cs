using Jotanunes.Domain.Entities;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class WorkerStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    WorkerGivenContext workerCtx)
{
    [Given(@"^que existe um trabalhador cadastrado para a empresa$")]
    public async Task DadoQueExisteUmTrabalhadorCadastradoParaAEmpresa()
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        workerCtx.IdTrabalhadorCadastrado = await CadastrarAsync(new Worker(idEmpresa, "José da Silva", "529.982.247-25"));
    }

    [Given(@"^que existe um trabalhador cadastrado para a outra empresa$")]
    public async Task DadoQueExisteUmTrabalhadorCadastradoParaAOutraEmpresa()
    {
        var idOutraEmpresa = companyCtx.IdOutraEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma outra empresa foi cadastrada - execute o Given 'que existe outra empresa cadastrada' antes.");

        workerCtx.IdTrabalhadorOutraEmpresa = await CadastrarAsync(new Worker(idOutraEmpresa, "Maria Lima", "111.444.777-35"));
    }

    [Given(@"^que o trabalhador cadastrado está alocado na solicitação cadastrada$")]
    public async Task DadoQueOTrabalhadorCadastradoEstaAlocadoNaSolicitacaoCadastrada()
    {
        await using var context = databaseFixture.CreateDbContext();

        var solicitacao = await context.SupplyRequests.SingleAsync(s => s.Id == supplyRequestCtx.IdSolicitacaoCadastrada);
        var trabalhador = await context.Workers.SingleAsync(w => w.Id == workerCtx.IdTrabalhadorCadastrado);

        context.WorkerAllocations.Add(new WorkerAllocation(solicitacao, trabalhador, 0));
        await context.SaveChangesAsync();
    }

    private async Task<long> CadastrarAsync(Worker trabalhador)
    {
        await using var context = databaseFixture.CreateDbContext();

        context.Workers.Add(trabalhador);
        await context.SaveChangesAsync();

        return trabalhador.Id;
    }
}
