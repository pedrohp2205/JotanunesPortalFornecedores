using AwesomeAssertions;

using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class EncerramentoSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    SupplyRequestGivenContext supplyRequestCtx,
    SupplyRequestResultContext supplyRequestResultCtx)
{
    [When(@"eu concluir a solicitação cadastrada")]
    public async Task QuandoEuConcluirASolicitacaoCadastrada()
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.CompleteAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu cancelar a solicitação cadastrada")]
    public async Task QuandoEuCancelarASolicitacaoCadastrada()
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.CancelAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu cancelar uma solicitação inexistente")]
    public async Task QuandoEuCancelarUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.CancelAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"^a solicitação retornada deve estar (concluída|cancelada)$")]
    public void EntaoASolicitacaoRetornadaDeveEstar(string situacao)
    {
        supplyRequestResultCtx.Solicitacao.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao!.Status.Should().Be(
            situacao == "concluída" ? SupplyRequestStatus.Completed : SupplyRequestStatus.Cancelled);
        supplyRequestResultCtx.Solicitacao.ClosedAt.Should().NotBeNull();
    }
}
