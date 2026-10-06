using System.Net.Http.Json;

using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class DocumentApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/Document";

    public async Task<PageListResponseDto<InternalDocumentDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<PageListResponseDto<InternalDocumentDto>>();
    }

    public async Task<InternalDocumentDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<InternalDocumentDto>();
    }

    public async Task<byte[]?> DownloadAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}/download");
        return httpResponseCtx.Response.IsSuccessStatusCode
            ? await httpResponseCtx.Response.Content.ReadAsByteArrayAsync()
            : null;
    }

    public async Task<InternalDocumentDto?> ApproveAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/{id}/approve", null);
        return await httpResponseCtx.TryReadFromJsonAsync<InternalDocumentDto>();
    }

    public async Task<InternalDocumentDto?> RejectAsync(long id, DocumentRejectDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync($"{BASE_URL}/{id}/reject", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<InternalDocumentDto>();
    }

    public async Task<ComplianceChecklistDto?> GetChecklistAsync(long supplyRequestId)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/checklist/{supplyRequestId}");
        return await httpResponseCtx.TryReadFromJsonAsync<ComplianceChecklistDto>();
    }

    public async Task<DocumentAnalysisDto?> GetAnalysisAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}/analysis");
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentAnalysisDto>();
    }

    public async Task<DocumentAnalysisDto?> ReanalyzeAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsync($"{BASE_URL}/{id}/analysis", null);
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentAnalysisDto>();
    }
}
