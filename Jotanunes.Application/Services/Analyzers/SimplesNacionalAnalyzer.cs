using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public partial class SimplesNacionalAnalyzer : IVisionAnalyzer
{
    private const string Label = "comprovante do Simples Nacional";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["SIMPLES_NACIONAL"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "comprovante de opção pelo Simples Nacional: consulta de optantes da Receita ou recibo/declaração do PGDAS-D",
        "cnpj: o CNPJ (matriz) do contribuinte",
        "isOptant: true se o documento mostra a empresa como optante pelo Simples Nacional, false se mostra que não é",
        "referenceDate: a data da consulta (consulta de optantes) ou o último dia do período de apuração (PGDAS-D)");

    public string SchemaName => "simples_nacional";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("cnpj", "string", "CNPJ do contribuinte"),
        ("isOptant", "boolean", "se é optante pelo Simples Nacional"),
        ("referenceDate", "string", "data de referência, dd/MM/yyyy"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        string? optant = null;
        string? referenceDate = null;

        if (Consultation().Match(flat) is { Success: true } consultation)
        {
            optant = consultation.Groups["not"].Success ? "false" : "true";
            referenceDate = ConsultationDate().Match(flat) is { Success: true } date ? date.Groups["date"].Value : null;
        }
        else if (PgdasOptant().Match(flat) is { Success: true } pgdas)
        {
            optant = TextPatterns.NormalizeName(pgdas.Groups["answer"].Value) == "SIM" ? "true" : "false";
            referenceDate = PgdasPeriodEnd().Match(flat) is { Success: true } period ? period.Groups["date"].Value
                : PgdasPeriod().Match(flat) is { Success: true } month && TextPatterns.ParseCompetence(month.Groups["comp"].Value) is { } competence
                    ? TextPatterns.FormatDate(competence.AddMonths(1).AddDays(-1))
                    : null;
        }

        extraction.Add("documentKind", optant is null ? null : "SIMPLES_NACIONAL", "identificação como comprovante do Simples Nacional");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ");
        extraction.Add("isOptant", optant, "situação no Simples Nacional");
        extraction.Add("referenceDate", referenceDate);

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

        extraction.Add("documentKind", "SIMPLES_NACIONAL");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("isOptant", VisionPrompt.GetBool(result, "isOptant") is { } optant ? (optant ? "true" : "false") : null, "situação no Simples Nacional");
        extraction.Add("referenceDate", TextPatterns.ParseDate(VisionPrompt.GetString(result, "referenceDate")) is { } date ? TextPatterns.FormatDate(date) : null);

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpjRoot(findings, extraction.Get("cnpj")?[..8], document.Company, Label);

        if (extraction.Get("isOptant") == "false")
        {
            findings.Add(new AnalysisFinding(
                "NOT_SIMPLES_OPTANT",
                FindingSeverity.Blocking,
                "O documento mostra que a empresa não é optante pelo Simples Nacional."));
        }

        if (TextPatterns.ParseDate(extraction.Get("referenceDate")) is { } reference && reference.Year != today.Year)
        {
            findings.Add(new AnalysisFinding(
                "NOT_CURRENT_YEAR",
                FindingSeverity.Warning,
                $"O comprovante é de {TextPatterns.FormatDate(reference)}; o checklist pede um comprovante do ano atual ({today.Year})."));
        }

        return findings;
    }

    [GeneratedRegex(@"Situa[cç][aã]o\s+no\s+Simples\s+Nacional:\s*(?<not>N[AÃ]O\s+)?optante", RegexOptions.IgnoreCase)]
    private static partial Regex Consultation();

    [GeneratedRegex(@"Data\s+da\s+consulta:\s*(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ConsultationDate();

    [GeneratedRegex(@"Optante\s+pelo\s+Simples\s+Nacional:?\s*(?:\d{2}/\d{2}/\d{4}\s+)?(?<answer>Sim|N[aã]o)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PgdasOptant();

    [GeneratedRegex(@"Per[ií]odo\s+de\s+Apura[cç][aã]o:\s*\d{2}/\d{2}/\d{4}\s+a\s+(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PgdasPeriodEnd();

    [GeneratedRegex(@"Per[ií]odo\s+de\s+Apura[cç][aã]o.{0,200}?(?<comp>(?:0[1-9]|1[0-2])/20\d{2})\s+\d{17}", RegexOptions.IgnoreCase)]
    private static partial Regex PgdasPeriod();
}
