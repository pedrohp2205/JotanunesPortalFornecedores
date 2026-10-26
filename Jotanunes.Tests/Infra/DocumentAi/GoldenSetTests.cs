using Jotanunes.Infra.DocumentAi.Services;
using Xunit.Abstractions;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class GoldenSetTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Should_Read_Annotated_Documents_As_Expected()
    {
        var folder = GoldenSet.FindFolder();
        if (folder is null)
        {
            output.WriteLine($"Pasta '{GoldenSet.FolderName}/golden-set.json' não encontrada; golden set ignorado.");
            return;
        }

        var cases = (await GoldenSet.LoadCases(folder)).Where(c => c.Engine is null).ToList();
        var extractor = new PdfNativeTextExtractor();
        var failures = new List<string>();

        foreach (var goldenCase in cases)
        {
            using var content = await GoldenSet.Open(folder, goldenCase);
            var extraction = GoldenSet.AnalyzerFor(goldenCase).Extract(await extractor.Extract(content, "application/pdf"));
            failures.AddRange(GoldenSet.Compare(goldenCase, extraction));
        }

        output.WriteLine($"{cases.Count - failures.Count}/{cases.Count} casos conferem.");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
