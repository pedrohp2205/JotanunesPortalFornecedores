using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services.Analyzers;

namespace Jotanunes.Tests.Infra.DocumentAi;

internal sealed record GoldenCase(
    string File,
    string DocumentTypeCode,
    string? Engine,
    bool ExpectComplete,
    bool ExpectWrongDocument,
    IReadOnlyDictionary<string, string> Expected,
    IReadOnlyDictionary<string, int> ExpectedListCounts);

internal static class GoldenSet
{
    public const string FolderName = "Documentos Jotanunes";

    public static readonly IDocumentTypeAnalyzer[] Analyzers =
    [
        new CrfAnalyzer(),
        new PaymentReceiptAnalyzer(),
        new PaymentProofAnalyzer(),
        new FgtsPaymentProofAnalyzer(),
        new FgtsDetailAnalyzer(),
        new FgtsGuideAnalyzer(),
        new TimesheetAnalyzer(),
        new EmployeeListAnalyzer(),
        new CnpjCardAnalyzer(),
        new FederalCndAnalyzer(),
        new SimplesNacionalAnalyzer(),
        new DctfWebAnalyzer(),
        new PayrollAnalyzer(),
        new SocialContractAnalyzer(),
        new AddressProofAnalyzer(),
        new PartnerIdAnalyzer(),
        .. GenericCertificateAnalyzer.Defaults()
    ];

    public static string? FindFolder()
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

    public static async Task<List<GoldenCase>> LoadCases(string folder)
    {
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "golden-set.json")));

        return json.RootElement.GetProperty("cases").EnumerateArray().Select(c => new GoldenCase(
            c.GetProperty("file").GetString()!,
            c.GetProperty("documentTypeCode").GetString()!,
            c.TryGetProperty("engine", out var engine) ? engine.GetString() : null,
            !c.TryGetProperty("expectComplete", out var complete) || complete.GetBoolean(),
            c.TryGetProperty("expectWrongDocument", out var wrong) && wrong.GetBoolean(),
            c.TryGetProperty("expected", out var expected)
                ? expected.EnumerateObject().ToDictionary(f => f.Name, f => f.Value.GetString()!)
                : new Dictionary<string, string>(),
            c.TryGetProperty("expectedListCounts", out var counts)
                ? counts.EnumerateObject().ToDictionary(f => f.Name, f => f.Value.GetInt32())
                : new Dictionary<string, int>())).ToList();
    }

    public static async Task<MemoryStream> Open(string folder, GoldenCase goldenCase)
    {
        var path = new[] { goldenCase.File, goldenCase.File.Normalize(System.Text.NormalizationForm.FormD), goldenCase.File.Normalize(System.Text.NormalizationForm.FormC) }
            .Select(file => Path.Combine(folder, file))
            .FirstOrDefault(File.Exists) ?? Path.Combine(folder, goldenCase.File);

        return new MemoryStream(await File.ReadAllBytesAsync(path));
    }

    public static IDocumentTypeAnalyzer AnalyzerFor(GoldenCase goldenCase)
    {
        return Analyzers.Single(a => a.DocumentTypeCodes.Contains(goldenCase.DocumentTypeCode));
    }

    public static IEnumerable<string> Compare(GoldenCase goldenCase, FieldExtraction extraction)
    {
        var label = $"{goldenCase.File} ({goldenCase.DocumentTypeCode})";

        if (goldenCase.ExpectWrongDocument)
        {
            if (extraction.WrongDocument is null)
            {
                yield return $"{label}: esperava que o arquivo fosse identificado como outro documento.";
            }

            yield break;
        }

        if (extraction.WrongDocument is not null)
        {
            yield return $"{label}: identificado como outro documento ({extraction.WrongDocument}).";
            yield break;
        }

        if (extraction.IsComplete != goldenCase.ExpectComplete)
        {
            yield return $"{label}: esperava leitura {(goldenCase.ExpectComplete ? "completa" : "incompleta")}; faltou [{string.Join(", ", extraction.Missing)}].";
            yield break;
        }

        foreach (var (name, count) in goldenCase.ExpectedListCounts)
        {
            var actual = extraction.GetList<JsonElement>(name).Count;
            if (actual != count)
            {
                yield return $"{label}: lista '{name}' esperava {count} item(ns), leu {actual}.";
            }
        }

        foreach (var (name, value) in goldenCase.Expected)
        {
            var actual = extraction.Get(name);
            if (actual != value)
            {
                yield return $"{label}: campo '{name}' esperado '{value}', lido '{actual}'.";
            }
        }
    }
}
