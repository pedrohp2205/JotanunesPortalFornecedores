using AwesomeAssertions;

using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class AtualizacaoSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    SupplyRequestGivenContext supplyRequestCtx,
    SupplyRequestResultContext supplyRequestResultCtx)
{
    [When(@"^eu atualizar a quantidade de trabalhadores da solicitação cadastrada para (\d+)$")]
    public async Task QuandoEuAtualizarAQuantidadeDeTrabalhadoresDaSolicitacaoCadastrada(int quantidade)
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.UpdateAsync(
            supplyRequestCtx.IdSolicitacaoCadastrada!.Value,
            new SupplyRequestUpdateDto { RequiredWorkerCount = quantidade });
    }

    [Then(@"^a solicitação retornada deve exigir (\d+) trabalhadores$")]
    public void EntaoASolicitacaoRetornadaDeveExigirTrabalhadores(int quantidade)
    {
        supplyRequestResultCtx.Solicitacao.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao!.RequiredWorkerCount.Should().Be(quantidade);
    }
}
