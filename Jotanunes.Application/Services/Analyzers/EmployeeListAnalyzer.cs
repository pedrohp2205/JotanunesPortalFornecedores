using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public record ListedEmployee(string Name, string? Cpf);

public partial class EmployeeListAnalyzer : IVisionAnalyzer
{
    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["EMPLOYEE_LIST"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "relação dos funcionários lotados em uma obra",
        "workSiteName: o nome da obra",
        "employees: um item por funcionário listado, com nome e CPF (null se o CPF não aparecer)");

    public string SchemaName => "employee_list";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("workSiteName", VisionPrompt.Nullable("string", "nome da obra")),
        VisionPrompt.List("employees", "funcionários listados",
            ("name", "string", "nome do funcionário"),
            ("cpf", "string", "CPF do funcionário")));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var lines = text.Pages
            .SelectMany(page => page.Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var titleIndex = lines.FindIndex(line => Title().IsMatch(line));
        var siteIndex = lines.FindIndex(line => WorkSite().IsMatch(line));

        extraction.Add("documentKind", titleIndex >= 0 ? "EMPLOYEE_LIST" : null, "identificação como relação de funcionários");
        extraction.Add("workSiteName", siteIndex >= 0 ? WorkSite().Match(lines[siteIndex]).Groups["site"].Value.Trim() : null, "nome da obra");

        var employees = lines
            .Skip(Math.Max(titleIndex, siteIndex) + 1)
            .Select(line => Employee().Match(line))
            .Where(match => match.Success)
            .Select(match => new ListedEmployee(match.Groups["name"].Value.Trim(), TextPatterns.ValidCpf(match.Groups["cpf"].Value)))
            .DistinctBy(employee => TextPatterns.NormalizeName(employee.Name))
            .ToList();
        extraction.AddList("employees", titleIndex >= 0 ? employees : [], "funcionários listados");

        return extraction;
    }

    public FieldExtraction FromVision(JsonElement result)
    {
        var extraction = new FieldExtraction();

        if (!VisionPrompt.IsExpected(result))
        {
            extraction.MarkWrongDocument(VisionPrompt.Detected(result));
            return extraction;
        }

        extraction.Add("documentKind", "EMPLOYEE_LIST");
        extraction.Add("workSiteName", VisionPrompt.GetString(result, "workSiteName"), "nome da obra");

        var employees = VisionPrompt.GetList(result, "employees")
            .Select(item => new ListedEmployee(
                VisionPrompt.GetString(item, "name") ?? string.Empty,
                TextPatterns.ValidCpf(VisionPrompt.GetString(item, "cpf"))))
            .Where(e => e.Name.Length > 0)
            .DistinctBy(e => TextPatterns.NormalizeName(e.Name))
            .ToList();
        extraction.AddList("employees", employees, "funcionários listados");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        var listed = extraction.Get("workSiteName")!;
        var workSite = document.SupplyRequest?.WorkSite?.Name;
        if (workSite is not null)
        {
            var normalizedListed = TextPatterns.NormalizeName(listed);
            var normalizedWorkSite = TextPatterns.NormalizeName(workSite);
            if (!normalizedListed.Contains(normalizedWorkSite, StringComparison.Ordinal)
                && !normalizedWorkSite.Contains(normalizedListed, StringComparison.Ordinal))
            {
                findings.Add(new AnalysisFinding(
                    "WORK_SITE_MISMATCH",
                    FindingSeverity.Warning,
                    $"A relação é da obra \"{listed}\", mas a solicitação é da obra \"{workSite}\"."));
            }
        }

        return findings;
    }

    [GeneratedRegex(@"RELA[CÇ][AÃ]O\s+D[OE]S?\s+FUNCION[AÁ]RIOS", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"(?:NOME\s+DA\s+)?OBRA\s*:\s*(?<site>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex WorkSite();

    [GeneratedRegex(@"^(?:\d+\s*[-.)]?\s*)?(?<name>[A-ZÀ-Ü][A-ZÀ-Ü']+(?:\s+[A-ZÀ-Ü][A-ZÀ-Ü']*)+)(?:\s*[-–]?\s*(?:CPF:?\s*)?(?<cpf>\d{3}\.?\d{3}\.?\d{3}-?\d{2}))?(?:\s+.*)?$")]
    private static partial Regex Employee();
}
