using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Enums;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.DocumentAi.Services;

public class TesseractOcrExtractor(
    PdfiumDocumentRasterizer rasterizer,
    ITesseractRunner tesseract,
    IOptions<OcrSettings> settings) : IDocumentTextExtractor
{
    private readonly OcrSettings _settings = settings.Value;

    public TextExtractionEngine Engine => TextExtractionEngine.Ocr;

    public async Task<DocumentText> Extract(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var images = await rasterizer.Render(content, contentType, _settings.Dpi, _settings.MaxPages, cancellationToken);
        var pages = new List<string>(images.Count);

        foreach (var image in images)
        {
            var text = await tesseract.Recognize(image.Content, cancellationToken);
            if (text is null)
            {
                return DocumentText.Empty;
            }

            pages.Add(text);
        }

        return new DocumentText(pages);
    }
}
