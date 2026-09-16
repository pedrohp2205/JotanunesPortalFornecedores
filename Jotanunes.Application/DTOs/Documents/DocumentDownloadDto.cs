namespace Jotanunes.Application.DTOs.Documents;

public class DocumentDownloadDto
{
    public Stream Content { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
