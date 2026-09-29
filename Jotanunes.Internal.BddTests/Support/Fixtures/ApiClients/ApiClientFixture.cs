namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class ApiClientFixture(ApiWebAppFactoryFixture apiWebAppFactory)
{
    private HttpClient? _client;

    public HttpClient Client => _client ??= apiWebAppFactory.CreateClient();
}
