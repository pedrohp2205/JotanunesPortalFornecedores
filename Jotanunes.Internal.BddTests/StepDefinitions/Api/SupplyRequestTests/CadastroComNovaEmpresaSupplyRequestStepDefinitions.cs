using AwesomeAssertions;

using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.SupplyRequestTests;

[Binding]
internal class CadastroComNovaEmpresaSupplyRequestStepDefinitions(
    SupplyRequestApiClientFixture supplyRequestFix,
    DatabaseFixture databaseFixture,
    WorkSiteGivenContext workSiteCtx,
    SupplyRequestResultContext supplyRequestResultCtx)
{
    [When(@"^eu abrir uma solicitação de (material|mão de obra) cadastrando a empresa com CNPJ ""(.*)"" e o acesso ""(.*)""$")]
    public async Task QuandoEuAbrirUmaSolicitacaoCadastrandoAEmpresaEOAcesso(string tipoFornecimento, string cnpj, string email)
    {
        var supplierType = SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento);

        supplyRequestResultCtx.Solicitacao = await supplyRequestFix.CreateWithNewCompanyAsync(new SupplyRequestWithNewCompanyCreateDto
        {
            Company = CompanyDomainDriver.CriarCompanyCreateDtoValido(cnpj, tipoFornecimento: supplierType),
            User = SupplierUserDomainDriver.CriarSupplierUserCreateDtoValido(email, "Provisoria@1"),
            WorkSiteId = workSiteCtx.IdObraCadastrada!.Value,
            SupplierType = supplierType,
            RequiredWorkerCount = supplierType == SupplierType.ManpowerLabor ? 3 : null
        });
    }

    [Then(@"a solicitação retornada deve estar aberta para a empresa com CNPJ ""(.*)""")]
    public async Task EntaoASolicitacaoRetornadaDeveEstarAbertaParaAEmpresaComCnpj(string cnpj)
    {
        supplyRequestResultCtx.Solicitacao.Should().NotBeNull();
        supplyRequestResultCtx.Solicitacao!.Status.Should().Be(SupplyRequestStatus.Open);

        await using var context = databaseFixture.CreateDbContext();
        var empresa = await context.Companies.SingleAsync(c => c.Id == supplyRequestResultCtx.Solicitacao.CompanyId);
        empresa.Cnpj.Should().Be(new string(cnpj.Where(char.IsDigit).ToArray()));
    }

    [Then(@"a empresa da solicitação deve ter o acesso ""(.*)"" exigindo troca de senha")]
    public async Task EntaoAEmpresaDaSolicitacaoDeveTerOAcessoExigindoTrocaDeSenha(string email)
    {
        await using var context = databaseFixture.CreateDbContext();
        var usuario = await context.SupplierUsers.SingleAsync(u => u.CompanyId == supplyRequestResultCtx.Solicitacao!.CompanyId);

        usuario.Email.Should().Be(email);
        usuario.Active.Should().BeTrue();
        usuario.MustChangePassword.Should().BeTrue();
    }
}
