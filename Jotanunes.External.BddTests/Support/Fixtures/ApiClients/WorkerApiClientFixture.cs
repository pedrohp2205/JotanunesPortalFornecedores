using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Workers;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class WorkerApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Worker";

    public async Task<PageListResponseDto<WorkerDto>?> GetAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync(BASE_URL);
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<WorkerDto>>();
    }

    public async Task<WorkerDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerDto>();
    }

    public async Task<WorkerDto?> CreateAsync(WorkerCreateDto worker)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, worker);
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerDto>();
    }

    public async Task<WorkerDto?> UpdateAsync(long id, WorkerUpdateDto worker)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}", worker);
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerDto>();
    }

    public async Task<WorkerDto?> DeactivateAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/{id}/deactivate", null);
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerDto>();
    }

    public async Task<List<WorkerAllocationDto>?> GetAllocationsAsync(long supplyRequestId, bool includeReleased = false)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{SupplyRequestApiClientFixture.BASE_URL}/{supplyRequestId}/workers?includeReleased={includeReleased}");
        return await httpResponseCtx.TryReadFromJsonAsync<List<WorkerAllocationDto>>();
    }

    public async Task<WorkerAllocationDto?> AllocateAsync(long supplyRequestId, long workerId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{SupplyRequestApiClientFixture.BASE_URL}/{supplyRequestId}/workers", new WorkerAllocateDto { WorkerId = workerId });
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerAllocationDto>();
    }

    public async Task<WorkerAllocationDto?> ReleaseAsync(long supplyRequestId, long workerId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .DeleteAsync($"{SupplyRequestApiClientFixture.BASE_URL}/{supplyRequestId}/workers/{workerId}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkerAllocationDto>();
    }
}
