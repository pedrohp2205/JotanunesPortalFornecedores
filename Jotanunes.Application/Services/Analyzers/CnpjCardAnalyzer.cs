using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public partial class CnpjCardAnalyzer : IVisionAnalyzer
{
    public const int MaxAgeDays = 90;
    private const string Label = "cartão CNPJ";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["CNPJ_CARD"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "Comprovante de Inscrição e de Situação Cadastral no CNPJ (cartão CNPJ), emitido pela Receita Federal",
        "cnpj: o \"Número de Inscrição\"",
        "corporateName: o \"Nome Empresarial\"",
        "registrationStatus: a \"Situação Cadastral\" (ATIVA, SUSPENSA, INAPTA, BAIXADA ou NULA)",
        "zipCode: o CEP do endereço",
        "mainActivity: o código e a descrição da atividade econômica principal",
        "issuedAt: a data em que o comprovante foi emitido");

    public string SchemaName => "cnpj_card";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("cnpj", "string", "número de inscrição"),
        ("corporateName", "string", "nome empresarial"),
        ("registrationStatus", "string", "situação cadastral"),
        ("zipCode", "string", "CEP"),
        ("mainActivity", "string", "atividade econômica principal"),
        ("issuedAt", "string", "data de emissão, dd/MM/yyyy"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "CNPJ_CARD" : null, "identificação como cartão CNPJ");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ");
        extraction.Add("corporateName", CorporateName().Match(flat) is { Success: true } name ? name.Groups["name"].Value.Trim() : null);
        extraction.Add("registrationStatus", Status().Match(flat) is { Success: true } status ? status.Groups["status"].Value.ToUpperInvariant() : null, "situação cadastral");
        extraction.Add("zipCode", ZipCode().Match(flat) is { Success: true } zip ? Digits(zip.Groups["zip"].Value) : null);
        extraction.Add("mainActivity", MainActivity().Match(flat) is { Success: true } activity ? activity.Groups["activity"].Value.Trim() : null);
        extraction.Add("issuedAt", IssuedAt().Match(flat) is { Success: true } issued ? issued.Groups["date"].Value : null, "data de emissão");

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

        extraction.Add("documentKind", "CNPJ_CARD");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("corporateName", VisionPrompt.GetString(result, "corporateName"));
        extraction.Add("registrationStatus", VisionPrompt.GetString(result, "registrationStatus")?.ToUpperInvariant(), "situação cadastral");
        extraction.Add("zipCode", Digits(VisionPrompt.GetString(result, "zipCode")) is { Length: 8 } zip ? zip : null);
        extraction.Add("mainActivity", VisionPrompt.GetString(result, "mainActivity"));
        extraction.Add("issuedAt", TextPatterns.ParseDate(VisionPrompt.GetString(result, "issuedAt")) is { } issued ? TextPatterns.FormatDate(issued) : null, "data de emissão");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();
        var company = document.Company;

        AnalysisChecks.CompanyCnpj(findings, extraction.Get("cnpj"), company, Label);

        var status = extraction.Get("registrationStatus")!;
        if (status != "ATIVA")
        {
            findings.Add(new AnalysisFinding(
                "REGISTRATION_NOT_ACTIVE",
                FindingSeverity.Blocking,
                $"A situação cadastral no CNPJ é {status}, e não ATIVA."));
        }

        AnalysisChecks.IssuedRecently(findings, TextPatterns.ParseDate(extraction.Get("issuedAt")), today, MaxAgeDays, Label);
        AnalysisChecks.CorporateName(findings, extraction.Get("corporateName"), company, Label);

        var zipCode = extraction.Get("zipCode");
        if (zipCode is not null && zipCode != Digits(company.Address.ZipCode))
        {
            findings.Add(new AnalysisFinding(
                "ZIP_CODE_MISMATCH",
                FindingSeverity.Warning,
                $"O CEP do cartão CNPJ ({zipCode}) difere do CEP cadastrado ({company.Address.ZipCode})."));
        }

        return findings;
    }

    private static string? Digits(string? value)
    {
        return value is null ? null : new string(value.Where(char.IsDigit).ToArray());
    }

    [GeneratedRegex(@"COMPROVANTE\s+DE\s+INSCRI[CÇ][AÃ]O\s+E\s+DE\s+SITUA[CÇ][AÃ]O\s+CADASTRAL|CADASTRO\s+NACIONAL\s+DA\s+PESSOA\s+JUR[IÍ]DICA", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"NOME\s+EMPRESARIAL\s+(?<name>.+?)\s+T[IÍ]TULO\s+DO\s+ESTABELECIMENTO", RegexOptions.IgnoreCase)]
    private static partial Regex CorporateName();

    [GeneratedRegex(@"SITUA[CÇ][AÃ]O\s+CADASTRAL\s+(?<status>ATIVA|SUSPENSA|INAPTA|BAIXADA|NULA)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Status();

    [GeneratedRegex(@"CEP\s+(?<zip>\d{2}\.?\d{3}-?\d{3})")]
    private static partial Regex ZipCode();

    [GeneratedRegex(@"ATIVIDADE\s+ECON[OÔ]MICA\s+PRINCIPAL\s+(?<activity>\d{2}\.\d{2}-\d-\d{2}\s+-\s+.+?)\s+C[OÓ]DIGO\s+E\s+DESCRI", RegexOptions.IgnoreCase)]
    private static partial Regex MainActivity();

    [GeneratedRegex(@"Emitido\s+no\s+dia\s+(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex IssuedAt();
}
