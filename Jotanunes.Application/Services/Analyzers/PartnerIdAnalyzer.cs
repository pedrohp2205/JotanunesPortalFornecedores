using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public class PartnerIdAnalyzer : IVisionAnalyzer
{
    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["PARTNER_ID"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "documento de identificação de um sócio da empresa: RG, CNH, carteira de identidade nova (CIN) ou documento com CPF",
        "documentType: RG, CNH, CIN ou OUTRO",
        "name e cpf: o nome e o CPF da pessoa",
        "validUntil: a data de validade, se o documento tiver (ex.: CNH)");

    public string SchemaName => "partner_id";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("documentType", "string", "RG, CNH, CIN ou OUTRO"),
        ("name", "string", "nome da pessoa"),
        ("cpf", "string", "CPF da pessoa"),
        ("validUntil", "string", "validade, dd/MM/yyyy"));

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

        extraction.Add("documentKind", "PARTNER_ID");
        extraction.Add("documentType", VisionPrompt.GetString(result, "documentType")?.ToUpperInvariant());
        extraction.Add("name", VisionPrompt.GetString(result, "name"), "nome");
        extraction.Add("cpf", TextPatterns.ValidCpf(VisionPrompt.GetString(result, "cpf")), "CPF");
        extraction.Add("validUntil", TextPatterns.ParseDate(VisionPrompt.GetString(result, "validUntil")) is { } valid ? TextPatterns.FormatDate(valid) : null);

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        if (TextPatterns.ParseDate(extraction.Get("validUntil")) is { } validUntil && validUntil < today)
        {
            findings.Add(new AnalysisFinding(
                "ID_EXPIRED",
                FindingSeverity.Warning,
                $"O documento ({extraction.Get("documentType") ?? "identificação"}) venceu em {TextPatterns.FormatDate(validUntil)}."));
        }

        return findings;
    }
}
