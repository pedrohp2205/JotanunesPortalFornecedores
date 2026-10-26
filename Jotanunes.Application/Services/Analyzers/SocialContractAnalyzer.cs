using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public record Partner(string Name, string? Cpf);

public partial class SocialContractAnalyzer : IVisionAnalyzer
{
    private const string Label = "contrato social";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["SOCIAL_CONTRACT"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "contrato social, alteração contratual ou consolidação de uma sociedade, normalmente registrado na Junta Comercial",
        "cnpj e corporateName: o CNPJ e o nome empresarial da sociedade",
        "registeredAt e registrationNumber: a data e o número do registro/arquivamento na Junta Comercial (\"Certifico o registro em ... sob nº ...\"); null se não houver registro",
        "partners: um item por sócio, com nome e CPF");

    public string SchemaName => "social_contract";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("cnpj", VisionPrompt.Nullable("string", "CNPJ da sociedade")),
        ("corporateName", VisionPrompt.Nullable("string", "nome empresarial")),
        ("registeredAt", VisionPrompt.Nullable("string", "data do registro na Junta, dd/MM/yyyy")),
        ("registrationNumber", VisionPrompt.Nullable("string", "número do registro/arquivamento")),
        VisionPrompt.List("partners", "sócios",
            ("name", "string", "nome do sócio"),
            ("cpf", "string", "CPF do sócio")));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "SOCIAL_CONTRACT" : null, "identificação como contrato social");
        extraction.Add("cnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ");
        extraction.Add("corporateName", CorporateName().Match(flat) is { Success: true } name ? name.Groups["name"].Value.Trim() : null);
        extraction.Add("nire", Nire().Match(flat) is { Success: true } nire ? nire.Groups["nire"].Value : null);

        var registration = Registration().Match(flat);
        extraction.Add("registeredAt", registration.Success ? registration.Groups["date"].Value : null);
        extraction.Add("registrationNumber", registration.Success && registration.Groups["number"].Success ? registration.Groups["number"].Value : null);

        var signers = Signer().Matches(flat).Select(m => new Partner(m.Groups["name"].Value.Trim(), TextPatterns.ValidCpf(m.Groups["cpf"].Value)));
        var qualified = Qualified().Matches(flat).Select(m => new Partner(m.Groups["name"].Value.Trim(), TextPatterns.ValidCpf(m.Groups["cpf"].Value)));
        extraction.AddList("partners", signers.Concat(qualified).Where(p => p.Cpf is not null).DistinctBy(p => p.Cpf).ToList());

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

        extraction.Add("documentKind", "SOCIAL_CONTRACT");
        extraction.Add("cnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "cnpj")), "CNPJ");
        extraction.Add("corporateName", VisionPrompt.GetString(result, "corporateName"));
        extraction.Add("registeredAt", TextPatterns.ParseDate(VisionPrompt.GetString(result, "registeredAt")) is { } date ? TextPatterns.FormatDate(date) : null);
        extraction.Add("registrationNumber", VisionPrompt.GetString(result, "registrationNumber"));
        extraction.AddList("partners", VisionPrompt.GetList(result, "partners")
            .Select(item => new Partner(VisionPrompt.GetString(item, "name") ?? string.Empty, TextPatterns.ValidCpf(VisionPrompt.GetString(item, "cpf"))))
            .Where(p => p.Name.Length > 0)
            .ToList());

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpj(findings, extraction.Get("cnpj"), document.Company, Label);
        AnalysisChecks.CorporateName(findings, extraction.Get("corporateName"), document.Company, Label);

        if (extraction.Get("registeredAt") is null)
        {
            findings.Add(new AnalysisFinding(
                "NOT_REGISTERED",
                FindingSeverity.Warning,
                "Não foi encontrado o registro na Junta Comercial (\"Certifico o registro em ...\"). Confira se o documento tem a chancela."));
        }

        if (extraction.GetList<Partner>("partners").Count == 0)
        {
            findings.Add(new AnalysisFinding(
                "PARTNERS_NOT_FOUND",
                FindingSeverity.Info,
                "Não foi possível identificar os sócios e seus CPFs no documento."));
        }

        return findings;
    }

    [GeneratedRegex(@"CONTRATO\s+SOCIAL|ALTERA[CÇ][AÃ]O\s+CONTRATUAL|CONSOLIDA[CÇ][AÃ]O\s+DO\s+CONTRATO|REQUERIMENTO\s+DE\s+EMPRES[AÁ]RIO", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"(?:DA\s+SOCIEDADE|nome\s+empresarial)\s+(?<name>[A-ZÀ-Ü0-9][A-ZÀ-Ü0-9&' .-]+?(?:LTDA|S/?A|EIRELI|ME|EPP)\b)")]
    private static partial Regex CorporateName();

    [GeneratedRegex(@"NIRE\s*(?:n[ºo°])?:?\s*(?<nire>\d{11})", RegexOptions.IgnoreCase)]
    private static partial Regex Nire();

    [GeneratedRegex(@"CERTIFICO\s+O\s+REGISTRO\s+EM\s+(?<date>\d{2}/\d{2}/\d{4})(?:\s+SOB\s+N[ºo°:.]*\s*(?<number>\d+))?", RegexOptions.IgnoreCase)]
    private static partial Regex Registration();

    [GeneratedRegex(@"[Cc][Pp][Ff]:\s*(?<cpf>\d{11})\s*-\s*(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s*-\s*Assinad[oa]")]
    private static partial Regex Signer();

    [GeneratedRegex(@"(?<name>[A-ZÀ-Ü][A-ZÀ-Ü']+(?:\s+[A-ZÀ-Ü][A-ZÀ-Ü']*)+),\s+[Bb]rasileir[oa].{0,300}?CPF\s*(?:n[ºo°])?:?\s*(?<cpf>\d{3}\.?\d{3}\.?\d{3}-?\d{2})")]
    private static partial Regex Qualified();
}
