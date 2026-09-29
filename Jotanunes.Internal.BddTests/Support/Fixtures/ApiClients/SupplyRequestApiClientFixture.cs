using System.Net.Http.Json;

using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class SupplyRequestApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/SupplyRequest";

    public async Task<PageListResponseDto<SupplyRequestDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<SupplyRequestDto>>();
    }

    public async Task<SupplyRequestDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }

    public async Task<SupplyRequestDto?> CreateAsync(SupplyRequestCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, dto);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }

    public async Task<SupplyRequestDto?> CreateWithNewCompanyAsync(SupplyRequestWithNewCompanyCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/with-new-company", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }

    public async Task<SupplyRequestDto?> UpdateAsync(long id, SupplyRequestUpdateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }

    public async Task<SupplyRequestDto?> CompleteAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/{id}/complete", null);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }

    public async Task<SupplyRequestDto?> CancelAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/{id}/cancel", null);
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }
}
