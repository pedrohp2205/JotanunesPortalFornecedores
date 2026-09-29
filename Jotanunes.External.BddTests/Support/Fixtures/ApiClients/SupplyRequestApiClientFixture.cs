using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class SupplyRequestApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/SupplyRequest";

    public async Task<PageListResponseDto<SupplyRequestDto>?> GetAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync(BASE_URL);
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<SupplyRequestDto>>();
    }

    public async Task<SupplyRequestDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<SupplyRequestDto>();
    }
}
