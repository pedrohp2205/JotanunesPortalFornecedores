using System.Net.Http.Json;

using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Internal.BddTests.Support.Contexts;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class WorkSiteApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/WorkSite";

    public async Task<List<WorkSiteDto>?> GetAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync(BASE_URL);
        return await httpResponseCtx.TryReadFromJsonAsync<List<WorkSiteDto>>();
    }

    public async Task<WorkSiteDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkSiteDto>();
    }

    public async Task<WorkSiteDto?> CreateAsync(WorkSiteCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, dto);
        return await httpResponseCtx.TryReadFromJsonAsync<WorkSiteDto>();
    }

    public async Task<WorkSiteDto?> UpdateAsync(long id, WorkSiteUpdateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<WorkSiteDto>();
    }

    public async Task<WorkSiteDto?> DeleteAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .DeleteAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<WorkSiteDto>();
    }
}
