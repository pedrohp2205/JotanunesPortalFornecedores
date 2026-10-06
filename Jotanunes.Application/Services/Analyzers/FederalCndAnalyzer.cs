using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public partial class FederalCndAnalyzer : IVisionAnalyzer, IExpiringDocumentAnalyzer
{
    public const string Negative = "NEGATIVA";
    public const string PositiveWithNegativeEffects = "POSITIVA_COM_EFEITOS_DE_NEGATIVA";
    public const string Positive = "POSITIVA";
    private const string Label = "certidão federal";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["FEDERAL_CND"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "certidão de débitos relativos a tributos federais e à dívida ativa da União (Receita Federal/PGFN)",
        $"certificateKind: {Negative} se for \"Certidão Negativa\", {PositiveWithNegativeEffects} se for \"Certidão Positiva com Efeitos de Negativa\", {Positive} se for só \"Certidão Positiva\"",
        "cnpj e holderName: o CNPJ e o nome do contribuinte",
        "issuedAt: a data de emissão",
        "validUntil: a data do \"Válida até\"",
        "controlCode: o código de controle da certidão");

    public string SchemaName => "federal_cnd";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("certificateKind", "string", $"{Negative}, {PositiveWithNegativeEffects} ou {Positive}"),
        ("cnpj", "string", "CNPJ do contribuinte"),
        ("holderName", "string", "nome do contribuinte"),
        ("issuedAt", "string", "data de emissão, dd/MM/yyyy"),
        ("validUntil", "string", "válida até, dd/MM/yyyy"),
        ("controlCode", "string", "código de controle"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        var kind = Kind().Match(flat);
        extraction.Add("documentKind", kind.Success ? "FEDERAL_CND" : null, "identificação como certidão de tributos federais");
        extraction.Add("certificateKind", kind.Success ? NormalizeKind(kind.Groups["kind"].Value) : null, "tipo da certidão");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ");
        extraction.Add("holderName", HolderName().Match(flat) is { Success: true } name ? name.Groups["name"].Value.Trim() : null);
        extraction.Add("issuedAt", IssuedAt().Match(flat) is { Success: true } issued ? issued.Groups["date"].Value : null);
        extraction.Add("validUntil", ValidUntil().Match(flat) is { Success: true } valid ? valid.Groups["date"].Value : null, "validade");
        extraction.Add("controlCode", ControlCode().Match(flat) is { Success: true } code ? code.Groups["code"].Value : null);

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

        var kind = VisionPrompt.GetString(result, "certificateKind");

        extraction.Add("documentKind", "FEDERAL_CND");
        extraction.Add("certificateKind", kind is Negative or PositiveWithNegativeEffects or Positive ? kind : null, "tipo da certidão");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("holderName", VisionPrompt.GetString(result, "holderName"));
        extraction.Add("issuedAt", Date(VisionPrompt.GetString(result, "issuedAt")));
        extraction.Add("validUntil", Date(VisionPrompt.GetString(result, "validUntil")), "validade");
        extraction.Add("controlCode", VisionPrompt.GetString(result, "controlCode"));

        return extraction;
    }

    public DateOnly? ReadExpirationDate(FieldExtraction extraction)
    {
        return TextPatterns.ParseDate(extraction.Get("validUntil"));
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpjRoot(findings, extraction.Get("cnpj")?[..8], document.Company, Label);

        switch (extraction.Get("certificateKind"))
        {
            case Positive:
                findings.Add(new AnalysisFinding(
                    "POSITIVE_CERTIFICATE",
                    FindingSeverity.Blocking,
                    "A certidão é POSITIVA: há débitos federais ou inscrição em dívida ativa sem exigibilidade suspensa."));
                break;
            case PositiveWithNegativeEffects:
                findings.Add(new AnalysisFinding(
                    "POSITIVE_WITH_NEGATIVE_EFFECTS",
                    FindingSeverity.Info,
                    "Certidão positiva com efeitos de negativa: há débitos com exigibilidade suspensa, e ela vale como negativa (arts. 205 e 206 do CTN)."));
                break;
        }

        if (TextPatterns.ParseDate(extraction.Get("validUntil")) is { } validUntil)
        {
            ValidityChecks.Add(findings, "certidão federal", null, validUntil, document.ExpirationDate, today);
        }

        AnalysisChecks.CorporateName(findings, extraction.Get("holderName"), document.Company, Label);

        return findings;
    }

    private static string NormalizeKind(string kind)
    {
        var normalized = TextPatterns.NormalizeName(kind);
        return normalized.Contains("EFEITOS", StringComparison.Ordinal) ? PositiveWithNegativeEffects
            : normalized.StartsWith("POSITIVA", StringComparison.Ordinal) ? Positive
            : Negative;
    }

    private static string? Date(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    [GeneratedRegex(@"CERTID[AÃ]O\s+(?<kind>NEGATIVA|POSITIVA\s+COM\s+EFEITOS\s+DE\s+NEGATIVA|POSITIVA)\s+DE\s+D[EÉ]BITOS\s+RELATIVOS\s+(?:AOS\s+)?(?:CR[EÉ]DITOS\s+)?TRIBUT", RegexOptions.IgnoreCase)]
    private static partial Regex Kind();

    [GeneratedRegex(@"Nome:\s*(?<name>.+?)\s+CNPJ:", RegexOptions.IgnoreCase)]
    private static partial Regex HolderName();

    [GeneratedRegex(@"Emitida\s+[aà]s\s+\d{2}:\d{2}:\d{2}\s+do\s+dia\s+(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex IssuedAt();

    [GeneratedRegex(@"V[aá]lida\s+at[eé]\s+(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ValidUntil();

    [GeneratedRegex(@"C[oó]digo\s+de\s+controle\s+da\s+certid[aã]o:\s*(?<code>[0-9A-F]{4}(?:\.[0-9A-F]{4}){3})", RegexOptions.IgnoreCase)]
    private static partial Regex ControlCode();
}
