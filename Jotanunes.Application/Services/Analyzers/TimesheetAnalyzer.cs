using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public partial class TimesheetAnalyzer : IVisionAnalyzer
{
    private const string Label = "folha de ponto";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["TIMESHEET"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "folha de ponto (espelho de ponto) mensal de um trabalhador",
        "employeeName e employeeCpf: o colaborador e o CPF dele",
        "employerCnpj: o CNPJ do empregador; null se o campo estiver vazio ou escrito \"null\"",
        "periodStart e periodEnd: o período da folha",
        "workedHours: o total de horas trabalhadas no período, em H:mm (ex.: 168:37)",
        "expectedHours: o total de horas previstas no período, em H:mm",
        "workplace: o local de trabalho ou da obra informado");

    public string SchemaName => "timesheet";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("employeeName", "string", "nome do colaborador"),
        ("employeeCpf", "string", "CPF do colaborador"),
        ("employerCnpj", "string", "CNPJ do empregador"),
        ("periodStart", "string", "início do período, dd/MM/yyyy"),
        ("periodEnd", "string", "fim do período, dd/MM/yyyy"),
        ("workedHours", "string", "total de horas trabalhadas, H:mm"),
        ("expectedHours", "string", "total de horas previstas, H:mm"),
        ("workplace", "string", "local de trabalho"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "TIMESHEET" : null, "identificação como folha de ponto");

        var name = CollaboratorName().Match(flat) is { Success: true } byBlock ? byBlock.Groups["name"].Value.Trim()
            : NameBeforeCpf().Match(flat) is { Success: true } byLabel ? byLabel.Groups["name"].Value.Trim()
            : null;
        extraction.Add("employeeName", name);
        extraction.Add("employeeCpf", Cpf().Match(flat) is { Success: true } cpf ? TextPatterns.ValidCpf(cpf.Groups["cpf"].Value) : null, "CPF do colaborador");
        extraction.Add("employerCnpj", EmployerCnpj().Match(flat) is { Success: true } cnpj ? TextPatterns.ValidCnpj(cnpj.Groups["cnpj"].Value) : null);
        extraction.Add("employerCnpjMissing", EmptyEmployerCnpj().IsMatch(flat) ? "true" : null);

        var period = Period().Match(flat);
        extraction.Add("periodStart", period.Success ? Date(period.Groups["from"].Value) : null, "início do período");
        extraction.Add("periodEnd", period.Success ? Date(period.Groups["until"].Value) : null, "fim do período");

        var totals = name is null ? Match.Empty : TotalsBeforeName(name).Match(flat);
        extraction.Add("workedHours", totals.Success ? Hours(totals.Groups["worked"].Value) : null);
        extraction.Add("expectedHours", totals.Success ? Hours(totals.Groups["expected"].Value) : null);
        extraction.Add("workplace", Workplace().Match(flat) is { Success: true } local ? local.Groups["local"].Value.Trim() : null);

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

        var employerCnpj = VisionPrompt.GetString(result, "employerCnpj");

        extraction.Add("documentKind", "TIMESHEET");
        extraction.Add("employeeName", VisionPrompt.GetString(result, "employeeName"));
        extraction.Add("employeeCpf", TextPatterns.ValidCpf(VisionPrompt.GetString(result, "employeeCpf")), "CPF do colaborador");
        extraction.Add("employerCnpj", TextPatterns.ValidCnpj(employerCnpj));
        extraction.Add("employerCnpjMissing", employerCnpj is null ? "true" : null);
        extraction.Add("periodStart", Date(VisionPrompt.GetString(result, "periodStart")), "início do período");
        extraction.Add("periodEnd", Date(VisionPrompt.GetString(result, "periodEnd")), "fim do período");
        extraction.Add("workedHours", Hours(VisionPrompt.GetString(result, "workedHours")));
        extraction.Add("expectedHours", Hours(VisionPrompt.GetString(result, "expectedHours")));
        extraction.Add("workplace", VisionPrompt.GetString(result, "workplace"));

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.WorkerCpf(findings, extraction.Get("employeeCpf"), document.Worker, Label);
        AnalysisChecks.WorkerName(findings, extraction.Get("employeeName"), document.Worker, Label);
        AnalysisChecks.PeriodOverlaps(
            findings,
            TextPatterns.ParseDate(extraction.Get("periodStart")),
            TextPatterns.ParseDate(extraction.Get("periodEnd")),
            document,
            Label);

        var employerCnpj = extraction.Get("employerCnpj");
        if (employerCnpj is not null)
        {
            AnalysisChecks.CompanyCnpj(findings, employerCnpj, document.Company, Label);
        }
        else if (extraction.Get("employerCnpjMissing") == "true")
        {
            findings.Add(new AnalysisFinding(
                "MISSING_EMPLOYER_CNPJ",
                FindingSeverity.Warning,
                "A folha de ponto não informa o CNPJ do empregador."));
        }

        var worked = TextPatterns.ParseHours(extraction.Get("workedHours"));
        var expected = TextPatterns.ParseHours(extraction.Get("expectedHours"));
        if (worked == 0)
        {
            findings.Add(new AnalysisFinding(
                "NO_WORKED_HOURS",
                FindingSeverity.Warning,
                expected is > 0
                    ? $"A folha de ponto soma 0:00 trabalhadas, de {TextPatterns.FormatHours(expected.Value)} previstas."
                    : "A folha de ponto soma 0:00 trabalhadas."));
        }

        var workplace = extraction.Get("workplace");
        var workSite = document.SupplyRequest?.WorkSite?.Name;
        if (workplace is not null && workSite is not null
            && !TextPatterns.NormalizeName(workplace).Contains(TextPatterns.NormalizeName(workSite), StringComparison.Ordinal))
        {
            findings.Add(new AnalysisFinding(
                "WORKPLACE_MISMATCH",
                FindingSeverity.Warning,
                $"O local da folha de ponto (\"{workplace}\") não menciona a obra da solicitação (\"{workSite}\")."));
        }

        return findings;
    }

    private static string? Date(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    private static string? Hours(string? value)
    {
        return TextPatterns.ParseHours(value) is { } minutes ? TextPatterns.FormatHours(minutes) : null;
    }

    private static Regex TotalsBeforeName(string name)
    {
        return new Regex($@"(?<worked>\d{{1,3}}:\d{{2}})\s+(?<expected>\d{{1,3}}:\d{{2}})\s+{Regex.Escape(name)}");
    }

    [GeneratedRegex(@"Folha\s+de\s+Ponto|Espelho\s+de\s+Ponto", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"DADOS\s+DO\s+COLABORADOR\s+(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s+Fun[cç][aã]o:")]
    private static partial Regex CollaboratorName();

    [GeneratedRegex(@"Nome:\s+(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s+CPF:")]
    private static partial Regex NameBeforeCpf();

    [GeneratedRegex(@"CPF:\s*(?<cpf>\d{3}\.?\d{3}\.?\d{3}-?\d{2})")]
    private static partial Regex Cpf();

    [GeneratedRegex(@"CNPJ:\s*(?<cnpj>\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2})")]
    private static partial Regex EmployerCnpj();

    [GeneratedRegex(@"CNPJ:\s*(null\b|Endere[cç]o:|Local:|$)", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyEmployerCnpj();

    [GeneratedRegex(@"(?<from>\d{2}/\d{2}/\d{4})\s+(?:a|at[eé])\s+(?<until>\d{2}/\d{2}/\d{4})")]
    private static partial Regex Period();

    [GeneratedRegex(@"Local:\s*(?<local>.+?)\s+(?:Nome:|DADOS|CPF:|$)")]
    private static partial Regex Workplace();
}
