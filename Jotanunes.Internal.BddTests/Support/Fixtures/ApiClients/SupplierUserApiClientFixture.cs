using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Users;
using Jotanunes.Internal.BddTests.Support.Contexts;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class SupplierUserApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    private static string BaseUrl(long companyId) => $"{CompanyApiClientFixture.BASE_URL}/{companyId}/users";

    public async Task<List<SupplierUserDto>?> GetByCompanyAsync(long companyId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync(BaseUrl(companyId));
        return await httpResponseCtx.TryReadFromJsonAsync<List<SupplierUserDto>>();
    }

    public async Task<SupplierUserDto?> CreateAsync(long companyId, SupplierUserCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BaseUrl(companyId), dto);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplierUserDto>();
    }

    public async Task<SupplierUserDto?> ActivateAsync(long companyId, long userId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BaseUrl(companyId)}/{userId}/activate", null);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplierUserDto>();
    }

    public async Task<SupplierUserDto?> DeactivateAsync(long companyId, long userId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BaseUrl(companyId)}/{userId}/deactivate", null);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplierUserDto>();
    }

    public async Task<SupplierUserDto?> ResetPasswordAsync(long companyId, long userId, SupplierUserResetPasswordDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BaseUrl(companyId)}/{userId}/reset-password", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplierUserDto>();
    }
}
