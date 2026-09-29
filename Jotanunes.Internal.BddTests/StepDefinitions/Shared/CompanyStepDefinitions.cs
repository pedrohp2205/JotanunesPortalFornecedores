using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class CompanyStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx)
{
    [Given(@"^que existe uma empresa cadastrada com CNPJ ""(.*)""$")]
    public async Task DadoQueExisteUmaEmpresaCadastradaComCnpj(string cnpj)
    {
        await DadoQueExisteUmaEmpresaCadastradaComCnpjQueFornece(cnpj, "material");
    }

    [Given(@"^que existe uma empresa cadastrada com CNPJ ""(.*)"" que fornece (material|mão de obra|material e mão de obra)$")]
    public async Task DadoQueExisteUmaEmpresaCadastradaComCnpjQueFornece(string cnpj, string tipoFornecimento)
    {
        await using var context = databaseFixture.CreateDbContext();

        var empresa = CompanyDomainDriver.CriarEmpresaValida(cnpj, tipoFornecimento: SupplyRequestDomainDriver.TipoFornecimento(tipoFornecimento));
        context.Companies.Add(empresa);
        await context.SaveChangesAsync();

        companyCtx.IdEmpresaCadastrada = empresa.Id;
        companyCtx.CnpjEmpresaCadastrada = empresa.Cnpj;
    }

    [Given(@"que existe outra empresa cadastrada")]
    public async Task DadoQueExisteOutraEmpresaCadastrada()
    {
        await using var context = databaseFixture.CreateDbContext();

        var empresa = CompanyDomainDriver.CriarEmpresaValida(TestConstants.CNPJ_OUTRA_EMPRESA, "Outra Construtora Ltda");
        context.Companies.Add(empresa);
        await context.SaveChangesAsync();

        companyCtx.IdOutraEmpresaCadastrada = empresa.Id;
    }
}
