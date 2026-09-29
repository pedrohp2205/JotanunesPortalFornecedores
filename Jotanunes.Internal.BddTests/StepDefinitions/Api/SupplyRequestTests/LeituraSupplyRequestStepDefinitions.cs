using AwesomeAssertions;

using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class LeituraSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx,
    SupplyRequestResultContext supplyRequestResultCtx)
{
    private PageListResponseDto<SupplyRequestDto>? _solicitacoes;

    [When(@"eu listar as solicitações da empresa cadastrada")]
    public async Task QuandoEuListarAsSolicitacoesDaEmpresaCadastrada()
    {
        _solicitacoes = await supplyRequestFix.GetAsync($"?companyId={companyCtx.IdEmpresaCadastrada}");
    }

    [When(@"eu consultar a solicitação cadastrada")]
    public async Task QuandoEuConsultarASolicitacaoCadastrada()
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.GetByIdAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu consultar uma solicitação inexistente")]
    public async Task QuandoEuConsultarUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de solicitações contém a solicitação cadastrada")]
    public void EntaoAListagemDeSolicitacoesContemASolicitacaoCadastrada()
    {
        _solicitacoes.Should().NotBeNull();
        _solicitacoes!.Items.Select(s => s.Id).Should().ContainSingle()
            .Which.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
    }

    [Then(@"^a solicitação retornada deve indicar (\d+) documentos de habilitação pendentes$")]
    public void EntaoASolicitacaoRetornadaDeveIndicarDocumentosDeHabilitacaoPendentes(int quantidade)
    {
        supplyRequestResultCtx.Solicitacao.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao!.Pending.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao.Pending!.MissingOnboardingCount.Should().Be(quantidade);
    }
}
