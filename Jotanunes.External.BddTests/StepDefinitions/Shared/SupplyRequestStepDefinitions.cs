using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class SupplyRequestStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    WorkSiteGivenContext workSiteCtx,
    SupplyRequestGivenContext supplyRequestCtx)
{
    [Given(@"^que existe uma solicitação de (material|mão de obra) (aberta|concluída|cancelada) para a empresa e a obra cadastradas$")]
    public async Task DadoQueExisteUmaSolicitacaoParaAEmpresaEAObraCadastradas(string tipoFornecimento, string situacao)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        supplyRequestCtx.IdSolicitacaoCadastrada = await CadastrarSolicitacaoAsync(idEmpresa, tipoFornecimento, situacao);
    }

    [Given(@"^que existe uma solicitação de material aberta para a outra empresa na obra cadastrada$")]
    public async Task DadoQueExisteUmaSolicitacaoDeMaterialAbertaParaAOutraEmpresaNaObraCadastrada()
    {
        var idOutraEmpresa = companyCtx.IdOutraEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma outra empresa foi cadastrada - execute o Given 'que existe outra empresa cadastrada' antes.");

        supplyRequestCtx.IdSolicitacaoOutraEmpresa = await CadastrarSolicitacaoAsync(idOutraEmpresa, "material", "aberta");
    }

    private async Task<long> CadastrarSolicitacaoAsync(long idEmpresa, string tipoFornecimento, string situacao)
    {
        var idObra = workSiteCtx.IdObraCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma obra foi cadastrada - execute o Given 'que existe uma obra cadastrada' antes.");

        await using var context = databaseFixture.CreateDbContext();

        var solicitacao = SupplyRequestDomainDriver.CriarSolicitacaoValida(idEmpresa, idObra, SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento));
        if (situacao == "concluída")
        {
            solicitacao.Complete();
        }
        else if (situacao == "cancelada")
        {
            solicitacao.Cancel();
        }

        context.SupplyRequests.Add(solicitacao);
        await context.SaveChangesAsync();

        return solicitacao.Id;
    }
}
