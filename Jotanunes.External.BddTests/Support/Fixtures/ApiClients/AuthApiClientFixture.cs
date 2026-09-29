using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class AuthApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Auth";

    public async Task<TokenDto?> LoginAsync(LoginDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/login", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<TokenDto>();
    }

    public async Task<AuthenticatedUserDto?> GetMeAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/me");
        return await httpResponseCtx.TryReadFromJsonAsync<AuthenticatedUserDto>();
    }
}
