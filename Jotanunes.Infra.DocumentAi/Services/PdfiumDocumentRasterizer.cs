using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SkiaSharp;

namespace Jotanunes.Infra.DocumentAi.Services;

public class PdfiumDocumentRasterizer(IOptions<DocumentImageSettings> settings) : IDocumentRasterizer
{
    private readonly DocumentImageSettings _settings = settings.Value;

    public Task<IReadOnlyList<DocumentImage>> Render(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        return Render(content, contentType, _settings.Dpi, _settings.MaxPages, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentImage>> Render(Stream content, string contentType, int dpi, int maxPages, CancellationToken cancellationToken = default)
    {
        if (contentType is "image/png" or "image/jpeg")
        {
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            return [new DocumentImage(copy.ToArray(), contentType)];
        }

        if (contentType != "application/pdf")
        {
            return [];
        }

        var pageCount = Conversion.GetPageCount(content, leaveOpen: true);
        content.Position = 0;
        var pages = SelectPages(pageCount, Math.Max(1, maxPages));
        var images = new List<DocumentImage>(pages.Count);

        foreach (var bitmap in Conversion.ToImages(content, pages, leaveOpen: true, options: new RenderOptions { Dpi = dpi }))
        {
            using (bitmap)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                images.Add(new DocumentImage(encoded.ToArray(), "image/png"));
            }
        }

        return images;
    }

    public static IReadOnlyList<int> SelectPages(int pageCount, int maxPages)
    {
        if (pageCount <= maxPages)
        {
            return Enumerable.Range(0, pageCount).ToList();
        }

        return Enumerable.Range(0, maxPages - 1).Append(pageCount - 1).ToList();
    }
}
