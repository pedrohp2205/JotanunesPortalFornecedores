using System.Text;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Options;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class PdfiumDocumentRasterizerTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static PdfiumDocumentRasterizer Rasterizer(int maxPages = 4) =>
        new(Options.Create(new DocumentImageSettings { MaxPages = maxPages, Dpi = 72 }));

    [Fact]
    public async Task Should_Render_Each_Pdf_Page_As_Png()
    {
        var images = await Rasterizer().Render(new MemoryStream(Pdf(pages: 2)), "application/pdf");

        Assert.Equal(2, images.Count);
        Assert.All(images, image =>
        {
            Assert.Equal("image/png", image.ContentType);
            Assert.Equal(PngSignature, image.Content[..8]);
        });
    }

    [Fact]
    public async Task Should_Stop_At_Max_Pages()
    {
        var images = await Rasterizer(maxPages: 1).Render(new MemoryStream(Pdf(pages: 3)), "application/pdf");

        Assert.Single(images);
    }

    [Fact]
    public async Task Should_Send_Uploaded_Images_As_They_Are()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0];

        var image = Assert.Single(await Rasterizer().Render(new MemoryStream(jpeg), "image/jpeg"));

        Assert.Equal(jpeg, image.Content);
        Assert.Equal("image/jpeg", image.ContentType);
    }

    [Fact]
    public async Task Should_Return_Nothing_For_Unknown_Content()
    {
        Assert.Empty(await Rasterizer().Render(new MemoryStream([1, 2, 3]), "application/octet-stream"));
    }

    private static byte[] Pdf(int pages)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages).Select(i => $"{3 + i} 0 R"))}] /Count {pages} >>"
        };
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
        foreach (var offset in offsets)
        {
            pdf.Append($"{offset:D10} 00000 n \n");
        }

        pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
