using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public partial class CrfAnalyzer : IVisionAnalyzer
{
    public const int ExpiringSoonDays = 7;

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["FGTS_CND"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "Certificado de Regularidade do FGTS (CRF), emitido pela Caixa Econômica Federal",
        "cnpj: o número de \"Inscrição\" do empregador",
        "corporateName: a \"Razão social\"",
        "validFrom e validUntil: as duas datas do período de \"Validade\"",
        "certificationNumber: o \"Certificação Número\"");

    public string SchemaName => "crf_fgts";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("cnpj", "string", "CNPJ da inscrição"),
        ("corporateName", "string", "razão social"),
        ("validFrom", "string", "início da validade, dd/MM/yyyy"),
        ("validUntil", "string", "fim da validade, dd/MM/yyyy"),
        ("certificationNumber", "string", "número da certificação"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();

        extraction.Add("documentKind", Title().IsMatch(text.Flat) ? "CRF" : null, "identificação como CRF do FGTS");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(text.Flat), "CNPJ");

        var validity = Validity().Match(text.Flat);
        extraction.Add("validFrom", validity.Success ? validity.Groups["from"].Value : null, "início da validade");
        extraction.Add("validUntil", validity.Success ? validity.Groups["until"].Value : null, "fim da validade");

        extraction.Add("corporateName", CorporateName().Match(text.Flat) is { Success: true } name ? name.Groups["name"].Value : null);
        extraction.Add("certificationNumber", CertificationNumber().Match(text.Flat) is { Success: true } number ? number.Groups["number"].Value : null);

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

        extraction.Add("documentKind", "CRF");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("validFrom", ValidDate(VisionPrompt.GetString(result, "validFrom")), "início da validade");
        extraction.Add("validUntil", ValidDate(VisionPrompt.GetString(result, "validUntil")), "fim da validade");
        extraction.Add("corporateName", VisionPrompt.GetString(result, "corporateName"));
        extraction.Add("certificationNumber", VisionPrompt.GetString(result, "certificationNumber"));

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        var cnpj = extraction.Get("cnpj")!;
        if (cnpj != document.Company.Cnpj)
        {
            findings.Add(new AnalysisFinding(
                "CNPJ_MISMATCH",
                FindingSeverity.Blocking,
                $"A CRF é do CNPJ {Cnpj.Format(cnpj)}, mas a empresa cadastrada é {Cnpj.Format(document.Company.Cnpj)}."));
        }

        var validFrom = TextPatterns.ParseDate(extraction.Get("validFrom"));
        var validUntil = TextPatterns.ParseDate(extraction.Get("validUntil"));

        if (validFrom is null || validUntil is null || validFrom > validUntil)
        {
            findings.Add(new AnalysisFinding(
                "INVALID_VALIDITY",
                FindingSeverity.Warning,
                $"Não foi possível interpretar a validade ({extraction.Get("validFrom")} a {extraction.Get("validUntil")})."));
        }
        else
        {
            AddValidityFindings(findings, validFrom.Value, validUntil.Value, document.ExpirationDate, today);
        }

        var corporateName = extraction.Get("corporateName");
        if (corporateName is not null
            && TextPatterns.NormalizeName(corporateName) != TextPatterns.NormalizeName(document.Company.CorporateName))
        {
            findings.Add(new AnalysisFinding(
                "CORPORATE_NAME_MISMATCH",
                FindingSeverity.Warning,
                $"Razão social na CRF (\"{corporateName}\") difere da cadastrada (\"{document.Company.CorporateName}\")."));
        }

        return findings;
    }

    private static string? ValidDate(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    private static void AddValidityFindings(
        List<AnalysisFinding> findings,
        DateOnly validFrom,
        DateOnly validUntil,
        DateOnly? informedExpiration,
        DateOnly today)
    {
        if (validUntil < today)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRED",
                FindingSeverity.Blocking,
                $"CRF vencida em {TextPatterns.FormatDate(validUntil)}."));
        }
        else if (validFrom > today)
        {
            findings.Add(new AnalysisFinding(
                "NOT_YET_VALID",
                FindingSeverity.Warning,
                $"A validade da CRF só começa em {TextPatterns.FormatDate(validFrom)}."));
        }
        else if (validUntil.DayNumber - today.DayNumber <= ExpiringSoonDays)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRING_SOON",
                FindingSeverity.Warning,
                $"CRF vence em {validUntil.DayNumber - today.DayNumber} dia(s), em {TextPatterns.FormatDate(validUntil)}."));
        }

        if (informedExpiration is null)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRATION_DATE_DETECTED",
                FindingSeverity.Info,
                $"Validade identificada na certidão: {TextPatterns.FormatDate(validUntil)}."));
        }
        else if (informedExpiration.Value != validUntil)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRATION_DATE_MISMATCH",
                FindingSeverity.Warning,
                $"A validade informada no envio ({TextPatterns.FormatDate(informedExpiration.Value)}) difere da certidão ({TextPatterns.FormatDate(validUntil)})."));
        }
    }

    [GeneratedRegex(@"Certificado\s+de\s+Regularidade\s+do\s+FGTS", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"Validade:?\s*(?<from>\d{2}/\d{2}/\d{4})\s*a\s*(?<until>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex Validity();

    [GeneratedRegex(@"Raz[aã]o\s+social:\s*(?<name>.+?)\s+Endere[cç]o:", RegexOptions.IgnoreCase)]
    private static partial Regex CorporateName();

    [GeneratedRegex(@"Certifica[cç][aã]o\s+N[uú]mero:?\s*(?<number>\d{10,})", RegexOptions.IgnoreCase)]
    private static partial Regex CertificationNumber();
}
