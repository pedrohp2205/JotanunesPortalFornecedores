using System.Diagnostics;
using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Jotanunes.Tests.Infra.DocumentAi;

[Trait("Category", "Eval")]
public class VisionGoldenSetTests(ITestOutputHelper output)
{
    private const string EnabledVariable = "JOTANUNES_VISION_GOLDEN";
    private const string ApiKeyVariable = "OPENROUTER_API_KEY";
    private const string ModelVariable = "OPENROUTER_MODEL";

    [Fact]
    public async Task Should_Read_Annotated_Scanned_Documents_With_The_Vision_Model()
    {
        var folder = GoldenSet.FindFolder();
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyVariable);

        if (folder is null || Environment.GetEnvironmentVariable(EnabledVariable) != "1" || string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine($"Ignorado: avaliação do modelo (Category=Eval). Exige a pasta do golden set, {EnabledVariable}=1 e {ApiKeyVariable}, e gasta créditos do OpenRouter.");
            return;
        }

        var settings = new OpenRouterSettings { ApiKey = apiKey };
        var model = Environment.GetEnvironmentVariable(ModelVariable);
        if (!string.IsNullOrWhiteSpace(model))
        {
            settings.Model = model;
        }

        using var http = new HttpClient { BaseAddress = new Uri(settings.BaseUrl), Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds) };
        var client = new OpenRouterVisionClient(http, Options.Create(settings), NullLogger<OpenRouterVisionClient>.Instance);
        var rasterizer = new PdfiumDocumentRasterizer(Options.Create(new DocumentImageSettings()));

        var cases = (await GoldenSet.LoadCases(folder)).Where(c => c.Engine == "vision").ToList();
        var failures = new List<string>();
        output.WriteLine($"Modelo: {settings.Model}");

        foreach (var goldenCase in cases)
        {
            var analyzer = (IVisionAnalyzer)GoldenSet.AnalyzerFor(goldenCase);
            using var content = await GoldenSet.Open(folder, goldenCase);
            var images = await rasterizer.Render(content, "application/pdf");

            var stopwatch = Stopwatch.StartNew();
            var json = await client.ExtractJson(new VisionRequest(analyzer.Instructions, analyzer.SchemaName, analyzer.JsonSchema, images));
            stopwatch.Stop();

            using var result = JsonDocument.Parse(json);
            var caseFailures = GoldenSet.Compare(goldenCase, analyzer.FromVision(result.RootElement)).ToList();
            failures.AddRange(caseFailures);

            output.WriteLine($"{(caseFailures.Count == 0 ? "OK  " : "FALHOU")} {goldenCase.File} · {images.Count} página(s) · {stopwatch.Elapsed.TotalSeconds:F1}s");
            output.WriteLine($"     {json}");
        }

        output.WriteLine($"{cases.Count - failures.Count}/{cases.Count} casos conferem.");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
