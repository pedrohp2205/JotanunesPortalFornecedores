using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Services.Analyzers;

public partial class DctfWebAnalyzer : IVisionAnalyzer
{
    private const string Label = "DCTFWeb";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["DCTFWEB_RECEIPT", "DCTFWEB_REPORT"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "recibo de entrega ou relatório da declaração DCTFWeb, da Receita Federal",
        "cnpj: o CNPJ do contribuinte",
        "competence: o período de apuração, em MM/yyyy",
        "totalDebits: o total de débitos apurados",
        "receiptNumber: o número do recibo de entrega");

    public string SchemaName => "dctfweb";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("cnpj", "string", "CNPJ do contribuinte"),
        ("competence", "string", "período de apuração, MM/yyyy"),
        ("totalDebits", "number", "total de débitos apurados, em reais"),
        ("receiptNumber", "string", "número do recibo"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "DCTFWEB" : null, "identificação como DCTFWeb");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ");
        extraction.Add("competence", Competence().Match(flat) is { Success: true } comp && TextPatterns.ParseCompetence(comp.Groups["comp"].Value) is { } month ? TextPatterns.FormatCompetence(month) : null, "período de apuração");
        extraction.Add("totalDebits", Total().Match(flat) is { Success: true } total && TextPatterns.ParseMoney(total.Groups["value"].Value) is { } amount ? TextPatterns.FormatAmount(amount) : null);
        extraction.Add("receiptNumber", Receipt().Match(flat) is { Success: true } receipt ? receipt.Groups["number"].Value : null);

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

        extraction.Add("documentKind", "DCTFWEB");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("competence", TextPatterns.ParseCompetence(VisionPrompt.GetString(result, "competence")) is { } month ? TextPatterns.FormatCompetence(month) : null, "período de apuração");
        extraction.Add("totalDebits", VisionPrompt.GetDecimal(result, "totalDebits") is { } total ? TextPatterns.FormatAmount(total) : null);
        extraction.Add("receiptNumber", VisionPrompt.GetString(result, "receiptNumber"));

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpjRoot(findings, extraction.Get("cnpj")?[..8], document.Company, Label);
        AnalysisChecks.CompetenceInPeriod(findings, TextPatterns.ParseCompetence(extraction.Get("competence")), document, Label);

        return findings;
    }

    [GeneratedRegex(@"DCTF\s*Web", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"Per[ií]odo\s+(?:de\s+)?apura[cç][aã]o\W{0,3}(?<comp>\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex Competence();

    [GeneratedRegex(@"\bTOTAL\s+R\$\s*(?<value>[\d.]+,\d{2})")]
    private static partial Regex Total();

    [GeneratedRegex(@"(?:N[ºo°]\s+do\s+recibo\s+de\s+entrega|N[uú]mero\s+do\s+Recibo)\W{0,3}(?<number>\d{10,})", RegexOptions.IgnoreCase)]
    private static partial Regex Receipt();
}
