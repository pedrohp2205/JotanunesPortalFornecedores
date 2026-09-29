using System.Net.Http.Json;

using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Application.DTOs.Companies;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class CompanyApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Company";

    public async Task<CompanyDto?> CreateAsync(CompanyCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, dto);
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }

    public async Task<CompanyDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }
}
