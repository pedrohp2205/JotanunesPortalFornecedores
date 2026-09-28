using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Jotanunes.Infra.DocumentAi.Services;

public class PdfNativeTextExtractor : IDocumentTextExtractor
{
    public TextExtractionEngine Engine => TextExtractionEngine.NativeText;

    public Task<DocumentText> Extract(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (contentType != "application/pdf")
        {
            return Task.FromResult(DocumentText.Empty);
        }

        using var pdf = PdfDocument.Open(content);

        var pages = new List<string>(pdf.NumberOfPages);
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(ContentOrderTextExtractor.GetText(page));
        }

        return Task.FromResult(new DocumentText(pages));
    }
}
