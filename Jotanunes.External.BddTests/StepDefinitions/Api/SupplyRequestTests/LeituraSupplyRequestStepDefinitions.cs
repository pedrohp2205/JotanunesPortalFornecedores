using AwesomeAssertions;

using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class LeituraSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    CompanyGivenContext companyCtx,
    SupplyRequestGivenContext supplyRequestCtx)
{
    private PageListResponseDto<SupplyRequestDto>? _solicitacoes;
    private SupplyRequestDto? _solicitacao;

    [When(@"eu listar as solicitações da minha empresa")]
    public async Task QuandoEuListarAsSolicitacoesDaMinhaEmpresa()
    {
        _solicitacoes = await supplyRequestFix.GetAsync();
    }

    [When(@"eu consultar a solicitação cadastrada")]
    public async Task QuandoEuConsultarASolicitacaoCadastrada()
    {
        _solicitacao = await supplyRequestFix.GetByIdAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu consultar a solicitação da outra empresa")]
    public async Task QuandoEuConsultarASolicitacaoDaOutraEmpresa()
    {
        await supplyRequestFix.GetByIdAsync(supplyRequestCtx.IdSolicitacaoOutraEmpresa!.Value);
    }

    [When(@"eu consultar uma solicitação inexistente")]
    public async Task QuandoEuConsultarUmaSolicitacaoInexistente()
    {
        await supplyRequestFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de solicitações contém apenas a solicitação da minha empresa")]
    public void EntaoAListagemDeSolicitacoesContemApenasASolicitacaoDaMinhaEmpresa()
    {
        _solicitacoes.Should().NotBeNull();
        _solicitacoes!.Items.Select(s => s.Id).Should().ContainSingle()
            .Which.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
    }

    [Then(@"a solicitação retornada deve ser a solicitação cadastrada com as pendências de documentação")]
    public void EntaoASolicitacaoRetornadaDeveSerASolicitacaoCadastradaComAsPendenciasDeDocumentacao()
    {
        _solicitacao.Should().NotBeNull();
        _solicitacao!.Id.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
        _solicitacao.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        _solicitacao.Pending.Should().NotBeNull();
        _solicitacao.Pending!.MissingOnboardingCount.Should().BePositive();
    }
}
