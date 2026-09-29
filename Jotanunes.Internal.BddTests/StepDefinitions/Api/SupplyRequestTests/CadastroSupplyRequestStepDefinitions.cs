using AwesomeAssertions;

using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class CadastroSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    CompanyGivenContext companyCtx,
    WorkSiteGivenContext workSiteCtx,
    SupplyRequestResultContext supplyRequestResultCtx)
{
    [When(@"^eu abrir uma solicitação de (material|mão de obra) para a empresa e a obra cadastradas$")]
    public async Task QuandoEuAbrirUmaSolicitacaoParaAEmpresaEAObraCadastradas(string tipoFornecimento)
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.CreateAsync(SupplyRequestDomainDriver.CriarSupplyRequestCreateDto(
            companyCtx.IdEmpresaCadastrada!.Value,
            workSiteCtx.IdObraCadastrada!.Value,
            SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento)));
    }

    [When(@"^eu abrir uma solicitação de (material|mão de obra) para a outra empresa na obra cadastrada$")]
    public async Task QuandoEuAbrirUmaSolicitacaoParaAOutraEmpresaNaObraCadastrada(string tipoFornecimento)
    {
        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.CreateAsync(SupplyRequestDomainDriver.CriarSupplyRequestCreateDto(
            companyCtx.IdOutraEmpresaCadastrada!.Value,
            workSiteCtx.IdObraCadastrada!.Value,
            SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento)));
    }

    [When(@"^eu abrir uma solicitação de (material|mão de obra) para a empresa cadastrada em uma obra inexistente$")]
    public async Task QuandoEuAbrirUmaSolicitacaoParaAEmpresaCadastradaEmUmaObraInexistente(string tipoFornecimento)
    {
        await supplyRequestFix.CreateAsync(SupplyRequestDomainDriver.CriarSupplyRequestCreateDto(
            companyCtx.IdEmpresaCadastrada!.Value,
            TestConstants.ID_INEXISTENTE,
            SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento)));
    }

    [Then(@"^a solicitação retornada deve estar aberta para o fornecimento de (material|mão de obra)$")]
    public void EntaoASolicitacaoRetornadaDeveEstarAbertaParaOFornecimentoDe(string tipoFornecimento)
    {
        supplyRequestResultCtx.Solicitacao.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao!.Id.Should().BePositive();
        supplyRequestResultCtx.Solicitacao.Status.Should().Be(SupplyRequestStatus.Open);
        supplyRequestResultCtx.Solicitacao.SupplierType.Should().Be(SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento));
        supplyRequestResultCtx.Solicitacao.CompanyId.Should().Be(companyCtx.IdEmpresaCadastrada);
        supplyRequestResultCtx.Solicitacao.WorkSiteId.Should().Be(workSiteCtx.IdObraCadastrada);
    }
}
