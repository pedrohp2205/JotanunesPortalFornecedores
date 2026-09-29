using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

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

        supplyRequestCtx.IdSolicitacaoCadastrada = solicitacao.Id;
    }
}
