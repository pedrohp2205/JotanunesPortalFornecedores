using System.Net.Http.Headers;
using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Auth;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class ApiClientFixture(ApiWebAppFactoryFixture apiWebAppFactory)
{
    private HttpClient? _client;

    public HttpClient Client => _client ??= apiWebAppFactory.CreateClient();

    public async Task<TokenDto> LoginAsSupplier(string email, string senha)
    {
        ResetAuthorization();

        var response = await Client.PostAsJsonAsync(
            AuthApiClientFixture.BASE_URL + "/login",
            new LoginDto { Email = email, Password = senha });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Falha ao autenticar '{email}' no cenário: {(int)response.StatusCode} {body}");
        }

        var token = await response.Content.ReadFromJsonAsync<TokenDto>()
            ?? throw new InvalidOperationException("Login não retornou token.");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return token;
    }

    public void ResetAuthorization()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    [AfterScenario]
    public void TeardownTest()
    {
        ResetAuthorization();
    }
}
