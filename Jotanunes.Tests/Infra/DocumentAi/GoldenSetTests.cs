using System.Text.Json;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Infra.DocumentAi.Services;
using Xunit.Abstractions;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class GoldenSetTests(ITestOutputHelper output)
{
    private const string FolderName = "Documentos Jotanunes";

    private static readonly IDocumentTypeAnalyzer[] Analyzers = [new CrfAnalyzer()];

    [Fact]
    public async Task Should_Read_Annotated_Documents_As_Expected()
    {
        var folder = FindFolder();
        if (folder is null)
        {
            output.WriteLine($"Pasta '{FolderName}/golden-set.json' não encontrada; golden set ignorado.");
            return;
        }

        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "golden-set.json")));
        var extractor = new PdfNativeTextExtractor();
        var failures = new List<string>();
        var cases = json.RootElement.GetProperty("cases").EnumerateArray().ToList();

        foreach (var goldenCase in cases)
        {
            var file = goldenCase.GetProperty("file").GetString()!;
            var code = goldenCase.GetProperty("documentTypeCode").GetString()!;
            var analyzer = Analyzers.Single(a => a.DocumentTypeCodes.Contains(code));

            await using var content = File.OpenRead(Path.Combine(folder, file));
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            buffer.Position = 0;

            var extraction = analyzer.Extract(await extractor.Extract(buffer, "application/pdf"));
            var expectComplete = goldenCase.GetProperty("expectComplete").GetBoolean();

            if (extraction.IsComplete != expectComplete)
            {
                failures.Add($"{file} ({code}): esperava leitura {(expectComplete ? "completa" : "incompleta")}; faltou [{string.Join(", ", extraction.Missing)}].");
                continue;
            }

            if (goldenCase.TryGetProperty("expected", out var expected))
            {
                foreach (var field in expected.EnumerateObject())
                {
                    var actual = extraction.Get(field.Name);
                    if (actual != field.Value.GetString())
                    {
                        failures.Add($"{file} ({code}): campo '{field.Name}' esperado '{field.Value.GetString()}', lido '{actual}'.");
                    }
                }
            }
        }

        output.WriteLine($"{cases.Count - failures.Count}/{cases.Count} casos conferem.");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static string? FindFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, FolderName);
            if (File.Exists(Path.Combine(candidate, "golden-set.json")))
            {
                return candidate;
            }
        }

        return null;
    }
}
