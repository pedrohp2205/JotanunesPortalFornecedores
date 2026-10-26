using System.Text;
using Jotanunes.Domain.Enums;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class TesseractOcrExtractorTests
{
    private static readonly IOptions<OcrSettings> Settings = Options.Create(new OcrSettings { Dpi = 72, MaxPages = 5 });

    private static TesseractOcrExtractor Extractor(ITesseractRunner runner) =>
        new(new PdfiumDocumentRasterizer(Options.Create(new DocumentImageSettings())), runner, Settings);

    private static byte[] Pdf(int pages)
    {
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages).Select(i => $"{3 + i} 0 R"))}] /Count {pages} >>" };
        objects.AddRange(Enumerable.Range(0, pages).Select(_ => "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"));
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        offsets.ForEach(offset => pdf.Append($"{offset:D10} 00000 n \n"));
        pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    [Fact]
    public async Task Should_Recognize_Each_Page()
    {
        var runner = new Mock<ITesseractRunner>();
        runner.SetupSequence(r => r.Recognize(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("página um")
            .ReturnsAsync("página dois");

        var text = await Extractor(runner.Object).Extract(new MemoryStream(Pdf(pages: 2)), "application/pdf");

        Assert.Equal(["página um", "página dois"], text.Pages);
        Assert.Equal(TextExtractionEngine.Ocr, Extractor(runner.Object).Engine);
    }

    [Fact]
    public async Task Should_Return_Empty_When_Tesseract_Is_Unavailable()
    {
        var runner = new Mock<ITesseractRunner>();
        runner.Setup(r => r.Recognize(It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var text = await Extractor(runner.Object).Extract(new MemoryStream(Pdf(pages: 1)), "application/pdf");

        Assert.True(text.IsEmpty);
    }

    [Fact]
    public async Task Should_Return_Null_When_Tesseract_Binary_Does_Not_Exist()
    {
        var runner = new TesseractCliRunner(
            Options.Create(new OcrSettings { TesseractPath = "/caminho/que/nao/existe/tesseract" }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TesseractCliRunner>.Instance);

        Assert.Null(await runner.Recognize([1, 2, 3]));
    }
}
