using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class LeituraEmpresaStepDefinitions(CompanyApiClientFixture companyFix)
{
    [When(@"eu consultar uma empresa inexistente")]
    public async Task QuandoEuConsultarUmaEmpresaInexistente()
    {
        await companyFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }
}
