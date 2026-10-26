using Jotanunes.Application.DTOs.Analysis;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentRasterizer
{
    Task<IReadOnlyList<DocumentImage>> Render(Stream content, string contentType, CancellationToken cancellationToken = default);
}
