using System.Globalization;
using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Services.Analyzers;

public class FgtsGuideAnalyzer : IVisionAnalyzer
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
        extraction.Add("documentKind", null, "leitura da guia por imagem");
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
}
