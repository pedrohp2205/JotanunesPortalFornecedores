using Jotanunes.Internal.BddTests.Support.Contexts;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

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
