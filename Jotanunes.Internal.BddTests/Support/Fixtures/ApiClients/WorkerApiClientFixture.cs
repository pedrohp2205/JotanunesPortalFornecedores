using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class WorkerApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Worker";

    public async Task<PageListResponseDto<WorkerDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<WorkerDto>>();
    }

    public async Task<WorkerDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerDto>();
    }

    public async Task<List<WorkerAllocationDto>?> GetAllocationsAsync(long supplyRequestId, bool includeReleased = false)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{SupplyRequestApiClientFixture.BASE_URL}/{supplyRequestId}/workers?includeReleased={includeReleased}");
        return await httpResponseCtx.TryReadFromJsonAsync<List<WorkerAllocationDto>>();
    }

    public async Task<WorkerAllocationDto?> ReleaseAsync(long supplyRequestId, long workerId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .DeleteAsync($"{SupplyRequestApiClientFixture.BASE_URL}/{supplyRequestId}/workers/{workerId}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerAllocationDto>();
    }
}
