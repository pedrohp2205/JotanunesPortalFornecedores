using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class DocumentApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Document";

    public async Task<PageListResponseDto<DocumentDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<DocumentDto>>();
    }

    public async Task<DocumentDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentDto>();
    }

    public async Task<byte[]?> DownloadAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}/download");
        return httpResponseCtx.Response.IsSuccessStatusCode
            ? await httpResponseCtx.Response.Content.ReadAsByteArrayAsync()
            : null;
    }

    public async Task<DocumentDto?> UploadAsync(MultipartFormDataContent form)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync(BASE_URL, form);
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentDto>();
    }

    public async Task<ComplianceChecklistDto?> GetChecklistAsync(long supplyRequestId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/checklist/{supplyRequestId}");
        return await httpResponseCtx.TryReadFromJsonAsync<ComplianceChecklistDto>();
    }
}
