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

    public async Task<IReadOnlyList<DocumentImage>> Render(Stream content, string contentType, CancellationToken cancellationToken = default)
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

        var maxPages = Math.Max(1, _settings.MaxPages);
        var images = new List<DocumentImage>();

        foreach (var bitmap in Conversion.ToImages(content, leaveOpen: true, options: new RenderOptions { Dpi = _settings.Dpi }))
        {
            using (bitmap)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                images.Add(new DocumentImage(encoded.ToArray(), "image/png"));
            }

            if (images.Count >= maxPages)
            {
                break;
            }
        }

        return images;
    }
}
