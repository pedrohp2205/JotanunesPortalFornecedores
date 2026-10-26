using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.DocumentAi.Services;

public class TesseractCliRunner(IOptions<OcrSettings> settings, ILogger<TesseractCliRunner> logger) : ITesseractRunner
{
    private readonly OcrSettings _settings = settings.Value;
    private int _unavailableLogged;

    public async Task<string?> Recognize(byte[] image, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(_settings.TesseractPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "stdin", "stdout", "-l", _settings.Language, "--psm", _settings.PageSegmentationMode.ToString(CultureInfo.InvariantCulture) })
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new Win32Exception("Processo do Tesseract não iniciou.");
        }
        catch (Win32Exception ex)
        {
            if (Interlocked.Exchange(ref _unavailableLogged, 1) == 0)
            {
                logger.LogWarning(ex, "Tesseract indisponível em '{Path}'; o nível de OCR será ignorado.", _settings.TesseractPath);
            }

            return null;
        }

        using (process)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));

            try
            {
                var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var error = process.StandardError.ReadToEndAsync(timeout.Token);

                await process.StandardInput.BaseStream.WriteAsync(image, timeout.Token);
                process.StandardInput.Close();

                await process.WaitForExitAsync(timeout.Token);

                if (process.ExitCode != 0)
                {
                    logger.LogWarning("Tesseract terminou com código {ExitCode}: {Error}", process.ExitCode, await error);
                    return null;
                }

                return await output;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                logger.LogWarning("Tesseract não terminou em {Timeout}s; página ignorada.", _settings.TimeoutSeconds);
                return null;
            }
        }
    }
}
