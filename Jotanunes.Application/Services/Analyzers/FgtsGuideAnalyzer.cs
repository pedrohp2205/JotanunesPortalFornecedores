using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Services.Analyzers;

public partial class FgtsGuideAnalyzer : IVisionAnalyzer
{
    private const string Label = "guia do FGTS";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["FGTS_REPORT"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "guia de recolhimento do FGTS Digital (GFD), emitida pelo Ministério do Trabalho",
        "employerCnpjRoot: o \"CPF/CNPJ do Empregador\" como aparece (normalmente só a raiz, ex.: 05.159.071)",
        "competence: a competência dos recolhimentos, em MM/yyyy",
        "declaredWorkers: a \"Quantidade Trabalhadores\"",
        "guideTotal: o \"Valor a recolher\" ou \"Total da Guia\"",
        "dueDate: o \"Pagar este documento até\"",
        "guideIdentifier: o \"Identificador\" da guia");

    public string SchemaName => "fgts_guide";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("employerCnpjRoot", "string", "CPF/CNPJ do empregador como aparece"),
        ("competence", "string", "competência, MM/yyyy"),
        ("declaredWorkers", "integer", "quantidade de trabalhadores"),
        ("guideTotal", "number", "valor a recolher, em reais"),
        ("dueDate", "string", "data limite de pagamento, dd/MM/yyyy"),
        ("guideIdentifier", "string", "identificador da guia"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) && GuideTotal().IsMatch(flat) ? "FGTS_REPORT" : null, "identificação como guia do FGTS");
        extraction.Add("employerCnpjRoot", EmployerRoot().Match(flat) is { Success: true } root ? new string(root.Groups["root"].Value.Where(char.IsDigit).ToArray()) : null, "CNPJ do empregador");

        var row = CompetenceRow().Match(flat);
        var competence = row.Success ? row.Groups["comp"].Value : MonthlyTag().Match(flat) is { Success: true } tag ? tag.Groups["comp"].Value : null;
        extraction.Add("competence", TextPatterns.ParseCompetence(competence) is { } month ? TextPatterns.FormatCompetence(month) : null, "competência");
        extraction.Add("declaredWorkers", row.Success ? row.Groups["count"].Value : null);
        extraction.Add("guideTotal", GuideTotal().Match(flat) is { Success: true } total && TextPatterns.ParseMoney(total.Groups["value"].Value) is { } amount ? TextPatterns.FormatAmount(amount) : null, "valor a recolher");
        extraction.Add("dueDate", DueDate().Match(flat) is { Success: true } due ? ValidDate(due.Groups["date"].Value) : null);
        extraction.Add("guideIdentifier", Identifier().Match(flat) is { Success: true } id ? id.Value : null);

        return extraction;
    }

    private static string? ValidDate(string value)
    {
        return TextPatterns.ParseDate(new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray())) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    public FieldExtraction FromVision(JsonElement result)
    {
        var extraction = new FieldExtraction();

        if (!VisionPrompt.IsExpected(result))
        {
            extraction.MarkWrongDocument(VisionPrompt.Detected(result));
            return extraction;
        }

        var root = new string((VisionPrompt.GetString(result, "employerCnpjRoot") ?? string.Empty).Where(char.IsDigit).ToArray());

        extraction.Add("documentKind", "FGTS_REPORT");
        extraction.Add("employerCnpjRoot", root.Length >= 8 ? root[..8] : null, "CNPJ do empregador");
        extraction.Add("competence", TextPatterns.ParseCompetence(VisionPrompt.GetString(result, "competence")) is { } month ? TextPatterns.FormatCompetence(month) : null, "competência");
        extraction.Add("guideTotal", VisionPrompt.GetDecimal(result, "guideTotal") is { } total ? TextPatterns.FormatAmount(total) : null, "valor a recolher");
        extraction.Add("dueDate", TextPatterns.ParseDate(VisionPrompt.GetString(result, "dueDate")) is { } due ? TextPatterns.FormatDate(due) : null, "vencimento");
        extraction.Add("declaredWorkers", VisionPrompt.GetDecimal(result, "declaredWorkers")?.ToString("0", CultureInfo.InvariantCulture));
        extraction.Add("guideIdentifier", VisionPrompt.GetString(result, "guideIdentifier"));

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpjRoot(findings, extraction.Get("employerCnpjRoot"), document.Company, Label);
        AnalysisChecks.CompetenceInPeriod(findings, TextPatterns.ParseCompetence(extraction.Get("competence")), document, Label);

        return findings;
    }

    [GeneratedRegex(@"FGTS\s+Digital|Guia\s+do\s+FGTS|\bGFD\b|recolhimentos?\s+do\s+FGTS|Fundo\s+de\s+Garantia", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"Empregador.{0,80}?(?<root>\d{2}\.\d{3}\.\d{3})(?![/\d])", RegexOptions.IgnoreCase)]
    private static partial Regex EmployerRoot();

    [GeneratedRegex(@"(?<comp>(?:0[1-9]|1[0-2])/20\d{2})\s+(?<count>\d{1,5})\s+[\d.]+,\d{2}")]
    private static partial Regex CompetenceRow();

    [GeneratedRegex(@"(?<comp>(?:0[1-9]|1[0-2])/20\d{2})\s+MENSAL", RegexOptions.IgnoreCase)]
    private static partial Regex MonthlyTag();

    [GeneratedRegex(@"Total\s+da\s+Guia:?\s*(?<value>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex GuideTotal();

    [GeneratedRegex(@"Pagar\s+este\s+documento\s+at[eé]\s*(?<date>\d{2}\s*/\s*\d{2}\s*/\s*\d{4})(?!\d)", RegexOptions.IgnoreCase)]
    private static partial Regex DueDate();

    [GeneratedRegex(@"\b\d{16}-\d\b")]
    private static partial Regex Identifier();
}
