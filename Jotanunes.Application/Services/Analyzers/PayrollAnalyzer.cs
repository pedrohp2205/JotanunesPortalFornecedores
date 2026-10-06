using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Services.Analyzers;

public record PayrollEmployee(string Name, string? NetPay, string? FgtsAmount);

public partial class PayrollAnalyzer : IVisionAnalyzer
{
    private const string Label = "folha de pagamento";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["PAYROLL"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "folha de pagamento mensal da empresa, com todos os empregados",
        "employerCnpj: o CNPJ da empresa",
        "competence: o mês/ano da folha, em MM/yyyy",
        "employees: um item por empregado, com o nome, o líquido a receber e o FGTS");

    public string SchemaName => "payroll";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("employerCnpj", VisionPrompt.Nullable("string", "CNPJ da empresa")),
        ("competence", VisionPrompt.Nullable("string", "competência, MM/yyyy")),
        VisionPrompt.List("employees", "empregados da folha",
            ("name", "string", "nome do empregado"),
            ("netPay", "number", "líquido a receber, em reais"),
            ("fgtsAmount", "number", "FGTS do mês, em reais")));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        var isPayroll = Title().IsMatch(flat) && !ReceiptTitle().IsMatch(flat);
        extraction.Add("documentKind", isPayroll ? "PAYROLL" : null, "identificação como folha de pagamento");
        extraction.Add("employerCnpj", TextPatterns.FirstValidCnpj(flat), "CNPJ da empresa");
        extraction.Add("competence", Competence().Match(flat) is { Success: true } comp && TextPatterns.ParseCompetence(comp.Groups["comp"].Value) is { } month ? TextPatterns.FormatCompetence(month) : null, "competência");

        var employees = ReadEmployees(flat);
        extraction.AddList("employees", employees, "empregados da folha");

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

        extraction.Add("documentKind", "PAYROLL");
        extraction.Add("employerCnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "employerCnpj")), "CNPJ da empresa");
        extraction.Add("competence", TextPatterns.ParseCompetence(VisionPrompt.GetString(result, "competence")) is { } month ? TextPatterns.FormatCompetence(month) : null, "competência");

        var employees = VisionPrompt.GetList(result, "employees")
            .Select(item => new PayrollEmployee(
                VisionPrompt.GetString(item, "name") ?? string.Empty,
                VisionPrompt.GetDecimal(item, "netPay") is { } net ? TextPatterns.FormatAmount(net) : null,
                VisionPrompt.GetDecimal(item, "fgtsAmount") is { } fgts ? TextPatterns.FormatAmount(fgts) : null))
            .Where(e => e.Name.Length > 0)
            .DistinctBy(e => TextPatterns.NormalizeName(e.Name))
            .ToList();
        extraction.AddList("employees", employees, "empregados da folha");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpj(findings, extraction.Get("employerCnpj"), document.Company, Label);
        AnalysisChecks.CompetenceInPeriod(findings, TextPatterns.ParseCompetence(extraction.Get("competence")), document, Label);

        return findings;
    }

    private static List<PayrollEmployee> ReadEmployees(string flat)
    {
        var headers = EmployeeHeader().Matches(flat);
        var employees = new List<PayrollEmployee>();

        for (var i = 0; i < headers.Count; i++)
        {
            var start = headers[i].Index + headers[i].Length;
            var end = i + 1 < headers.Count ? headers[i + 1].Index : flat.Length;
            var block = flat[start..end];
            if (TotalMarker().Match(block) is { Success: true } total)
            {
                block = block[..total.Index];
            }

            var amounts = Amounts().Match(block);
            employees.Add(new PayrollEmployee(
                headers[i].Groups["name"].Value.Trim(),
                amounts.Success ? Amount(amounts.Groups["net"].Value) : null,
                amounts.Success ? Amount(amounts.Groups["fgts"].Value) : null));
        }

        return employees.DistinctBy(e => TextPatterns.NormalizeName(e.Name)).ToList();
    }

    private static string? Amount(string value)
    {
        return TextPatterns.ParseMoney(value) is { } amount ? TextPatterns.FormatAmount(amount) : null;
    }

    [GeneratedRegex(@"Folha\s+de\s+Pagamento", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"Recibo\s+de\s+Pagamento", RegexOptions.IgnoreCase)]
    private static partial Regex ReceiptTitle();

    [GeneratedRegex(@"(?:M[eê]s/Ano|Compet[eê]ncia):?\s*(?<comp>(?:0[1-9]|1[0-2])/20\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Competence();

    [GeneratedRegex(@"\b\d{6}\s+[^A-Za-zÀ-ÿ0-9\s]{0,3}\s*(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s+Cargo:")]
    private static partial Regex EmployeeHeader();

    [GeneratedRegex(@"FGTS:\s*(?<fgts>[\d.]+,\d{2})\s+L[ií]quido\s+a\s+receber:\s*(?<net>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Amounts();

    [GeneratedRegex(@"Total\s+Geral|Totais\s+da\s+Folha|Resumo\s+da\s+Folha", RegexOptions.IgnoreCase)]
    private static partial Regex TotalMarker();
}
