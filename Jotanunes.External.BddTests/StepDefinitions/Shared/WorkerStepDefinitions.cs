using Jotanunes.Domain.Entities;
using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class WorkerStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    WorkerGivenContext workerCtx)
{
    [Given(@"^que existe um trabalhador ""(.*)"" com CPF ""(.*)"" cadastrado para a empresa$")]
    public async Task DadoQueExisteUmTrabalhadorCadastradoParaAEmpresa(string nome, string cpf)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        workerCtx.IdTrabalhadorCadastrado = await CadastrarAsync(WorkerDomainDriver.CriarTrabalhadorValido(idEmpresa, nome, cpf));
    }

    [Given(@"^que existe um trabalhador cadastrado para a empresa$")]
    public async Task DadoQueExisteUmTrabalhadorCadastradoParaAEmpresa()
    {
        await DadoQueExisteUmTrabalhadorCadastradoParaAEmpresa("José da Silva", WorkerDomainDriver.CPF_PADRAO);
    }

    [Given(@"^que existe um trabalhador cadastrado para a outra empresa$")]
    public async Task DadoQueExisteUmTrabalhadorCadastradoParaAOutraEmpresa()
    {
        var idOutraEmpresa = companyCtx.IdOutraEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma outra empresa foi cadastrada - execute o Given 'que existe outra empresa cadastrada' antes.");

        workerCtx.IdTrabalhadorOutraEmpresa = await CadastrarAsync(WorkerDomainDriver.CriarTrabalhadorValido(idOutraEmpresa, "Maria Lima", WorkerDomainDriver.CPF_OUTRO));
    }

    [Given(@"^que o trabalhador cadastrado está alocado na solicitação cadastrada$")]
    public async Task DadoQueOTrabalhadorCadastradoEstaAlocadoNaSolicitacaoCadastrada()
    {
        var idTrabalhador = workerCtx.IdTrabalhadorCadastrado
            ?? throw new InvalidOperationException(
                "Nenhum trabalhador foi cadastrado - execute o Given 'que existe um trabalhador ... cadastrado para a empresa' antes.");
        var idSolicitacao = supplyRequestCtx.IdSolicitacaoCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma solicitação foi cadastrada - execute o Given 'que existe uma solicitação ...' antes.");

        await using var context = databaseFixture.CreateDbContext();

        var solicitacao = await context.SupplyRequests.SingleAsync(s => s.Id == idSolicitacao);
        var trabalhador = await context.Workers.SingleAsync(w => w.Id == idTrabalhador);
        var ativos = await context.WorkerAllocations.CountAsync(a => a.SupplyRequestId == idSolicitacao && a.ReleasedAt == null);

        context.WorkerAllocations.Add(new WorkerAllocation(solicitacao, trabalhador, ativos));
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
