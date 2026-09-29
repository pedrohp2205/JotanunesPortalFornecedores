using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

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
