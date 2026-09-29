using System.Net.Http.Json;

using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Internal.BddTests.Support.Contexts;

namespace Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class DocumentTypeApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/DocumentType";

    public async Task<List<DocumentTypeDto>?> GetAsync()
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync(BASE_URL);
        return await httpResponseCtx.TryReadFromJsonAsync<List<DocumentTypeDto>>();
    }

    public async Task<DocumentTypeDto?> GetByIdAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentTypeDto>();
    }

    public async Task<DocumentTypeDto?> CreateAsync(DocumentTypeCreateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PostAsJsonAsync(BASE_URL, dto);
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentTypeDto>();
    }

    public async Task<DocumentTypeDto?> UpdateAsync(long id, DocumentTypeUpdateDto? dto)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .PutAsJsonAsync($"{BASE_URL}/{id}", dto);
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentTypeDto>();
    }

    public async Task<DocumentTypeDto?> DeleteAsync(long id)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .DeleteAsync($"{BASE_URL}/{id}");
        return await httpResponseCtx.TryReadFromJsonAsync<DocumentTypeDto>();
    }
}
