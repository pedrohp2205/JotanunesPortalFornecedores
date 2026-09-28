using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Application.Services;

public static class DocumentFileInspector
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private static readonly (byte[] Signature, string ContentType)[] AllowedSignatures =
    [
        ("%PDF-"u8.ToArray(), "application/pdf"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png"),
        ([0xFF, 0xD8, 0xFF], "image/jpeg")
    ];

    private static readonly int HeaderLength = AllowedSignatures.Max(s => s.Signature.Length);

    public static async Task<string> Inspect(Stream content)
    {
        JotanunesException.When(!content.CanSeek, "Não foi possível ler o arquivo enviado.");
        JotanunesException.When(content.Length == 0, "O arquivo enviado está vazio.");
        JotanunesException.When(
            content.Length > MaxFileSizeBytes,
            $"O arquivo excede o tamanho máximo de {MaxFileSizeBytes / (1024 * 1024)} MB.");

        var header = new byte[HeaderLength];
        content.Position = 0;
        var read = await content.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false);
        content.Position = 0;

        foreach (var (signature, contentType) in AllowedSignatures)
        {
            if (read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                return contentType;
            }
        }

        throw new JotanunesException("Formato de arquivo não aceito. Envie PDF, PNG ou JPEG.");
    }
}
