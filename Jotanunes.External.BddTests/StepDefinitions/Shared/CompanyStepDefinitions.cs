using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class CompanyStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx)
{
    [Given(@"que existe uma empresa cadastrada com CNPJ ""(.*)""")]
    public async Task DadoQueExisteUmaEmpresaCadastradaComCnpj(string cnpj)
    {
        await using var context = databaseFixture.CreateDbContext();

        var empresa = CompanyDomainDriver.CriarEmpresaValida(cnpj);
        context.Companies.Add(empresa);
        await context.SaveChangesAsync();

        companyCtx.IdEmpresaCadastrada = empresa.Id;
        companyCtx.CnpjEmpresaCadastrada = empresa.Cnpj;
    }
}
