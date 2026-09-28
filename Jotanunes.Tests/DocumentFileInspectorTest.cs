using Jotanunes.Application.Services;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests;

public class DocumentFileInspectorTest
{
    private static MemoryStream File(params byte[] bytes) => new(bytes);

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 }, "application/pdf")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }, "image/jpeg")]
    public async Task Should_Detect_Content_Type_From_File_Signature(byte[] bytes, string expected)
    {
        var stream = File(bytes);

        var contentType = await DocumentFileInspector.Inspect(stream);

        Assert.Equal(expected, contentType);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Should_Reject_File_With_Unknown_Signature()
    {
        var ex = await Assert.ThrowsAsync<JotanunesException>(() => DocumentFileInspector.Inspect(File(0x4D, 0x5A, 0x90, 0x00, 0x03)));

        Assert.Equal("Formato de arquivo não aceito. Envie PDF, PNG ou JPEG.", ex.Message);
    }

    [Fact]
    public async Task Should_Reject_Empty_File()
    {
        var ex = await Assert.ThrowsAsync<JotanunesException>(() => DocumentFileInspector.Inspect(File()));

        Assert.Equal("O arquivo enviado está vazio.", ex.Message);
    }

    [Fact]
    public async Task Should_Reject_File_Above_Size_Limit()
    {
        var stream = new MemoryStream();
        stream.Write("%PDF-"u8);
        stream.SetLength(DocumentFileInspector.MaxFileSizeBytes + 1);

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => DocumentFileInspector.Inspect(stream));

        Assert.StartsWith("O arquivo excede o tamanho máximo", ex.Message);
    }
}
