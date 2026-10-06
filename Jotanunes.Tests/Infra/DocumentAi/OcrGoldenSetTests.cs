using System.Diagnostics;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class OcrGoldenSetTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Should_Read_Annotated_Scanned_Documents_With_Ocr()
    {
        var folder = GoldenSet.FindFolder();
        if (folder is null || !TesseractInstalled())
        {
            output.WriteLine("Ignorado: exige a pasta do golden set e o Tesseract instalado (tesseract-ocr + tesseract-ocr-por).");
            return;
        }

        var settings = Options.Create(new OcrSettings());
        var extractor = new TesseractOcrExtractor(
            new PdfiumDocumentRasterizer(Options.Create(new DocumentImageSettings())),
            new TesseractCliRunner(settings, NullLogger<TesseractCliRunner>.Instance),
            settings);

        var cases = (await GoldenSet.LoadCases(folder)).Where(c => c.Engine == "ocr").ToList();
        var failures = new List<string>();

        foreach (var goldenCase in cases)
        {
            using var content = await GoldenSet.Open(folder, goldenCase);
            var stopwatch = Stopwatch.StartNew();
            var text = await extractor.Extract(content, "application/pdf");
            var caseFailures = GoldenSet.Compare(goldenCase, GoldenSet.AnalyzerFor(goldenCase).Extract(text)).ToList();
            failures.AddRange(caseFailures);
            output.WriteLine($"{(caseFailures.Count == 0 ? "OK  " : "FALHOU")} {goldenCase.File} · {text.Pages.Count} página(s) · {stopwatch.Elapsed.TotalSeconds:F1}s");
        }

        output.WriteLine($"{cases.Count - failures.Count}/{cases.Count} casos conferem.");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static bool TesseractInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("tesseract", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            process!.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
