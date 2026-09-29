using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class CompanyApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Company";

    public async Task<PageListResponseDto<CompanyDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<CompanyDto>>();
    }

    public async Task<CompanyDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }

    public async Task<CompanyDto?> CreateAsync(CompanyCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, dto);
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }

    public async Task<CompanyDto?> UpdateAsync(long id, CompanyUpdateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }

    public async Task<CompanyDto?> ChangeSupplierTypeAsync(long id, CompanyChangeSupplierTypeDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}/supplier-type", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }

    public async Task<CompanyDto?> DeleteAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .DeleteAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<CompanyDto>();
    }
}
