namespace Jotanunes.Application.Interfaces;

public interface IDocumentStorageService
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
