namespace Jotanunes.Infra.DocumentAi.Services;

public interface ITesseractRunner
{
    Task<string?> Recognize(byte[] image, CancellationToken cancellationToken = default);
}
