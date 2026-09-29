using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.TestingEnvironmentSetup;

[Binding]
internal class VerificaConfiguracaoDaApiStepDefinitions(HealthApiClientFixture healthFix)
{
    [When(@"eu consultar a saúde da API")]
    public async Task QuandoEuConsultarASaudeDaApi()
    {
        await healthFix.GetAsync();
    }
}
