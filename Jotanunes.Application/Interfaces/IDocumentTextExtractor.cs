using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentTextExtractor
{
    TextExtractionEngine Engine { get; }
    Task<DocumentText> Extract(Stream content, string contentType, CancellationToken cancellationToken = default);
}
