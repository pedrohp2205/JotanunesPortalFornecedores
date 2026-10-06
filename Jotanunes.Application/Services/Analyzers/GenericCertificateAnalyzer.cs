using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public class GenericCertificateAnalyzer : IVisionAnalyzer
{
    private readonly string _label;

    public GenericCertificateAnalyzer(string documentTypeCode, string expectedDocument, string label)
    {
        DocumentTypeCodes = [documentTypeCode];
        SchemaName = documentTypeCode.ToLowerInvariant();
        _label = label;
        Instructions = VisionPrompt.Instructions(
            expectedDocument,
            "holderName e holderDocument: a empresa ou pessoa a quem o documento se refere e o CNPJ/CPF dela",
            "documentNumber: o número do documento",
            "issuer: o órgão ou entidade que emitiu",
            "issuedAt e validUntil: as datas de emissão e de validade; null se não houver");
    }

    public static IEnumerable<GenericCertificateAnalyzer> Defaults()
    {
        yield return new GenericCertificateAnalyzer(
            "MUNICIPAL_LICENSE",
            "licença de operação municipal (ou ambiental) emitida pela prefeitura ou órgão municipal",
            "licença municipal");
        yield return new GenericCertificateAnalyzer(
            "CGCRE_CERTIFICATION",
            "certificado de acreditação de laboratório pela Cgcre/Inmetro",
            "certificação Cgcre/Inmetro");
        yield return new GenericCertificateAnalyzer(
            "ART",
            "Anotação de Responsabilidade Técnica (ART) do CREA, ou RRT do CAU",
            "ART");
    }

    public IReadOnlyCollection<string> DocumentTypeCodes { get; }

    public string Instructions { get; }

    public string SchemaName { get; }

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("holderName", "string", "titular do documento"),
        ("holderDocument", "string", "CNPJ ou CPF do titular"),
        ("documentNumber", "string", "número do documento"),
        ("issuer", "string", "órgão emissor"),
        ("issuedAt", "string", "data de emissão, dd/MM/yyyy"),
        ("validUntil", "string", "data de validade, dd/MM/yyyy"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        extraction.Add("documentKind", null, "leitura do documento por imagem");
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

        extraction.Add("documentKind", DocumentTypeCodes.First());
        extraction.Add("holderName", VisionPrompt.GetString(result, "holderName"), "titular");
        extraction.Add("holderDocument", VisionPrompt.GetString(result, "holderDocument"));
        extraction.Add("documentNumber", VisionPrompt.GetString(result, "documentNumber"));
        extraction.Add("issuer", VisionPrompt.GetString(result, "issuer"));
        extraction.Add("issuedAt", Date(VisionPrompt.GetString(result, "issuedAt")));
        extraction.Add("validUntil", Date(VisionPrompt.GetString(result, "validUntil")));

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        if (TextPatterns.ValidCnpj(extraction.Get("holderDocument")) is { } cnpj && cnpj[..8] != document.Company.Cnpj[..8])
        {
            findings.Add(new AnalysisFinding(
                "CNPJ_MISMATCH",
                FindingSeverity.Blocking,
                $"A {_label} é do CNPJ {Cnpj.Format(cnpj)}, mas a empresa cadastrada é {Cnpj.Format(document.Company.Cnpj)}."));
        }

        if (TextPatterns.ParseDate(extraction.Get("validUntil")) is { } validUntil)
        {
            ValidityChecks.Add(findings, _label, null, validUntil, document.ExpirationDate, today);
        }

        return findings;
    }

    private static string? Date(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }
}
