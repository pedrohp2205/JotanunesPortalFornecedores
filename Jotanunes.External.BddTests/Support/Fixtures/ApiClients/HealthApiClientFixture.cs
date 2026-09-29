using Jotanunes.External.BddTests.Support.Contexts;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class HealthApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Health";

    public async Task GetAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client.GetAsync(BASE_URL);
    }
}
