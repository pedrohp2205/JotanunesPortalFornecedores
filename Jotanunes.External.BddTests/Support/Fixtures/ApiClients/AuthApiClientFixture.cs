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

    public async Task<TokenDto?> RefreshAsync(RefreshTokenDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/refresh", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<TokenDto>();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/forgot-password", dto);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/reset-password", dto);
    }

    public async Task LogoutAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/logout", null);
    }

    public async Task<AuthenticatedUserDto?> GetMeAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/me");
        return await httpResponseCtx.TryReadFromJsonAsync<AuthenticatedUserDto>();
    }

    public async Task<TokenDto?> ChangePasswordAsync(ChangePasswordDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/change-password", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<TokenDto>();
    }
}
