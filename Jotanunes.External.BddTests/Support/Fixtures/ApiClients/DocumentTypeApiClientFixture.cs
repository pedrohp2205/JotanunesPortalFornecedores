using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.External.BddTests.Support.Contexts;

namespace Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

[Binding]
internal class DocumentTypeApiClientFixture(
    HttpResponseContext httpResponseCtx,
    ApiClientFixture apiClientFixture)
{
    public const string BASE_URL = "/api/DocumentType";

    public async Task<List<SupplierDocumentTypeDto>?> GetAsync(string? queryString = null)
    {
        httpResponseCtx.Response = await apiClientFixture.Client
            .GetAsync($"{BASE_URL}{queryString}");
        return await httpResponseCtx.TryReadFromJsonAsync<List<SupplierDocumentTypeDto>>();
    }
}
