using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.WorkerTests;

[Binding]
internal class LeituraWorkerStepDefinitions(
    WorkerApiClientFixture workerFix,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    WorkerGivenContext workerCtx,
    WorkerResultContext workerResultCtx)
{
    [When(@"^eu listar os trabalhadores da empresa cadastrada$")]
    public async Task QuandoEuListarOsTrabalhadoresDaEmpresaCadastrada()
    {
        workerResultCtx.Trabalhadores = await workerFix.GetAsync($"?CompanyId={companyCtx.IdEmpresaCadastrada}");
    }

    [When(@"^eu consultar o trabalhador cadastrado$")]
    public async Task QuandoEuConsultarOTrabalhadorCadastrado()
    {
        workerResultCtx.Trabalhador = await workerFix.GetByIdAsync(workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [When(@"^eu consultar um trabalhador inexistente$")]
    public async Task QuandoEuConsultarUmTrabalhadorInexistente()
    {
        workerResultCtx.Trabalhador = await workerFix.GetByIdAsync(-1);
    }

    [When(@"^eu listar os trabalhadores alocados na solicitação cadastrada$")]
    public async Task QuandoEuListarOsTrabalhadoresAlocadosNaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacoes = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"^eu desalocar o trabalhador cadastrado da solicitação cadastrada$")]
    public async Task QuandoEuDesalocarOTrabalhadorCadastradoDaSolicitacaoCadastrada()
    {
        workerResultCtx.Alocacao = await workerFix.ReleaseAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value, workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [Then(@"^a listagem de trabalhadores contém apenas o trabalhador da empresa cadastrada$")]
    public void EntaoAListagemDeTrabalhadoresContemApenasOTrabalhadorDaEmpresaCadastrada()
    {
        workerResultCtx.Trabalhadores.Should().NotBeNull();
        workerResultCtx.Trabalhadores!.Items.Select(w => w.Id).Should().Equal(workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [Then(@"^o trabalhador retornado deve ser o trabalhador cadastrado$")]
    public void EntaoOTrabalhadorRetornadoDeveSerOTrabalhadorCadastrado()
    {
        workerResultCtx.Trabalhador.Should().NotBeNull();
        workerResultCtx.Trabalhador!.Id.Should().Be(workerCtx.IdTrabalhadorCadastrado);
        workerResultCtx.Trabalhador.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        workerResultCtx.Trabalhador.FormattedCpf.Should().Be("529.982.247-25");
    }

    [Then(@"^a listagem de alocados contém apenas o trabalhador cadastrado$")]
    public void EntaoAListagemDeAlocadosContemApenasOTrabalhadorCadastrado()
    {
        workerResultCtx.Alocacoes.Should().NotBeNull();
        workerResultCtx.Alocacoes!.Select(a => a.WorkerId).Should().Equal(workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [Then(@"^a solicitação cadastrada não deve ter trabalhadores alocados$")]
    public async Task EntaoASolicitacaoCadastradaNaoDeveTerTrabalhadoresAlocados()
    {
        var alocacoes = await workerFix.GetAllocationsAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);

        alocacoes.Should().NotBeNull().And.BeEmpty();
    }
}
