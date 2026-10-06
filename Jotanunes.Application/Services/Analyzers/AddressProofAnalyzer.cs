using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public class AddressProofAnalyzer : IVisionAnalyzer
{
    public const int MaxAgeDays = 90;
    private const string Label = "comprovante de endereço";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["ADDRESS_PROOF_COMPANY"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "comprovante de endereço comercial (conta de luz, água, telefone, internet, ou contrato de locação do imóvel)",
        "holderName e holderDocument: o titular (cliente ou locatário) e o CPF/CNPJ dele",
        "street, number, city, state e zipCode: o endereço do imóvel",
        "issuedAt: a data de emissão; se não houver, o primeiro dia do mês de referência",
        "isLeaseContract: true se o documento for um contrato de locação");

    public string SchemaName => "address_proof";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("holderName", "string", "nome do titular"),
        ("holderDocument", "string", "CPF ou CNPJ do titular"),
        ("street", "string", "logradouro"),
        ("number", "string", "número"),
        ("city", "string", "cidade"),
        ("state", "string", "UF"),
        ("zipCode", "string", "CEP"),
        ("issuedAt", "string", "data de emissão, dd/MM/yyyy"),
        ("isLeaseContract", "boolean", "se é contrato de locação"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        extraction.Add("documentKind", null, "leitura do comprovante por imagem");
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

        var zipCode = new string((VisionPrompt.GetString(result, "zipCode") ?? string.Empty).Where(char.IsDigit).ToArray());

        extraction.Add("documentKind", "ADDRESS_PROOF_COMPANY");
        extraction.Add("holderName", VisionPrompt.GetString(result, "holderName"), "titular");
        extraction.Add("holderDocument", VisionPrompt.GetString(result, "holderDocument"));
        extraction.Add("street", VisionPrompt.GetString(result, "street"));
        extraction.Add("number", VisionPrompt.GetString(result, "number"));
        extraction.Add("city", VisionPrompt.GetString(result, "city"));
        extraction.Add("state", VisionPrompt.GetString(result, "state"));
        extraction.Add("zipCode", zipCode.Length == 8 ? zipCode : null, "CEP");
        extraction.Add("issuedAt", TextPatterns.ParseDate(VisionPrompt.GetString(result, "issuedAt")) is { } issued ? TextPatterns.FormatDate(issued) : null);
        extraction.Add("isLeaseContract", VisionPrompt.GetBool(result, "isLeaseContract") is true ? "true" : null);

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();
        var company = document.Company;

        var holder = extraction.Get("holderName")!;
        var holderCnpj = TextPatterns.ValidCnpj(extraction.Get("holderDocument"));
        var holderIsCompany = holderCnpj is not null
            ? holderCnpj[..8] == company.Cnpj[..8]
            : SameName(holder, company.CorporateName) || SameName(holder, company.TradeName);

        if (!holderIsCompany)
        {
            findings.Add(new AnalysisFinding(
                "HOLDER_NOT_COMPANY",
                FindingSeverity.Warning,
                $"O titular do comprovante é \"{holder}\", e não a empresa ({company.CorporateName}). Confira se é sócio ou se há contrato de locação."));
        }

        var zipCode = extraction.Get("zipCode")!;
        if (zipCode != new string(company.Address.ZipCode.Where(char.IsDigit).ToArray()))
        {
            findings.Add(new AnalysisFinding(
                "ZIP_CODE_MISMATCH",
                FindingSeverity.Warning,
                $"O CEP do comprovante ({zipCode}) difere do CEP cadastrado ({company.Address.ZipCode})."));
        }

        var number = extraction.Get("number");
        if (number is not null && TextPatterns.NormalizeName(number) != TextPatterns.NormalizeName(company.Address.Number))
        {
            findings.Add(new AnalysisFinding(
                "ADDRESS_NUMBER_MISMATCH",
                FindingSeverity.Warning,
                $"O número do endereço no comprovante ({number}) difere do cadastrado ({company.Address.Number})."));
        }

        if (extraction.Get("isLeaseContract") != "true")
        {
            AnalysisChecks.IssuedRecently(findings, TextPatterns.ParseDate(extraction.Get("issuedAt")), today, MaxAgeDays, Label);
        }

        return findings;
    }

    private static bool SameName(string name, string companyName)
    {
        var normalized = TextPatterns.NormalizeName(name);
        var normalizedCompany = TextPatterns.NormalizeName(companyName);
        return normalized.Length > 0 && normalizedCompany.Length > 0
            && (normalized.Contains(normalizedCompany, StringComparison.Ordinal) || normalizedCompany.Contains(normalized, StringComparison.Ordinal));
    }
}
