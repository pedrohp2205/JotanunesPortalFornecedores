using AwesomeAssertions;

using Jotanunes.Application.DTOs.Workers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.WorkerTests;

[Binding]
internal class CadastroWorkerStepDefinitions(
    WorkerApiClientFixture workerFix,
    CompanyGivenContext companyCtx,
    WorkerGivenContext workerCtx,
    WorkerResultContext workerResultCtx)
{
    [When(@"^eu cadastrar o trabalhador ""(.*)"" com CPF ""(.*)""$")]
    public async Task QuandoEuCadastrarOTrabalhador(string nome, string cpf)
    {
        workerResultCtx.Trabalhador = await workerFix.CreateAsync(new WorkerCreateDto { Name = nome, Cpf = cpf });
    }

    [When(@"^eu listar os trabalhadores da minha empresa$")]
    public async Task QuandoEuListarOsTrabalhadoresDaMinhaEmpresa()
    {
        workerResultCtx.Trabalhadores = await workerFix.GetAsync();
    }

    [When(@"^eu consultar o trabalhador cadastrado$")]
    public async Task QuandoEuConsultarOTrabalhadorCadastrado()
    {
        workerResultCtx.Trabalhador = await workerFix.GetByIdAsync(workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [When(@"^eu consultar o trabalhador da outra empresa$")]
    public async Task QuandoEuConsultarOTrabalhadorDaOutraEmpresa()
    {
        workerResultCtx.Trabalhador = await workerFix.GetByIdAsync(workerCtx.IdTrabalhadorOutraEmpresa!.Value);
    }

    [When(@"^eu alterar o nome do trabalhador cadastrado para ""(.*)""$")]
    public async Task QuandoEuAlterarONomeDoTrabalhadorCadastrado(string nome)
    {
        workerResultCtx.Trabalhador = await workerFix.UpdateAsync(workerCtx.IdTrabalhadorCadastrado!.Value, new WorkerUpdateDto { Name = nome });
    }

    [When(@"^eu desativar o trabalhador cadastrado$")]
    public async Task QuandoEuDesativarOTrabalhadorCadastrado()
    {
        workerResultCtx.Trabalhador = await workerFix.DeactivateAsync(workerCtx.IdTrabalhadorCadastrado!.Value);
    }

    [Then(@"^o trabalhador retornado deve estar ativo com CPF ""(.*)""$")]
    public void EntaoOTrabalhadorRetornadoDeveEstarAtivoComCpf(string cpf)
    {
        workerResultCtx.Trabalhador.Should().NotBeNull();
        workerResultCtx.Trabalhador!.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        workerResultCtx.Trabalhador.Cpf.Should().Be(cpf);
        workerResultCtx.Trabalhador.Active.Should().BeTrue();
    }

    [Then(@"^o trabalhador retornado deve se chamar ""(.*)""$")]
    public void EntaoOTrabalhadorRetornadoDeveSeChamar(string nome)
    {
        workerResultCtx.Trabalhador.Should().NotBeNull();
        workerResultCtx.Trabalhador!.Name.Should().Be(nome);
    }

    [Then(@"^o trabalhador retornado deve estar inativo$")]
    public void EntaoOTrabalhadorRetornadoDeveEstarInativo()
    {
        workerResultCtx.Trabalhador.Should().NotBeNull();
        workerResultCtx.Trabalhador!.Active.Should().BeFalse();
    }

    [Then(@"^a listagem de trabalhadores contém apenas o trabalhador da minha empresa$")]
    public void EntaoAListagemDeTrabalhadoresContemApenasOTrabalhadorDaMinhaEmpresa()
    {
        workerResultCtx.Trabalhadores.Should().NotBeNull();
        workerResultCtx.Trabalhadores!.Items.Select(w => w.Id).Should().Equal(workerCtx.IdTrabalhadorCadastrado!.Value);
    }
}
