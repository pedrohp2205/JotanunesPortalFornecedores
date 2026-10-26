using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public record FgtsWorker(string Name, string Cpf, string? BaseAmount, string? FgtsAmount);

public partial class FgtsDetailAnalyzer : IVisionAnalyzer
{
    private const string Label = "detalhamento do FGTS";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["FGTS_DETAIL"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "detalhamento da guia do FGTS Digital (\"Detalhe da Guia Emitida\"), com a relação de trabalhadores",
        "employerCnpjRoot: a raiz do CNPJ do empregador (8 primeiros dígitos, ex.: 05.159.071)",
        "competence: a competência (\"Comp. Apuração\"), em MM/yyyy",
        "dueDate: o vencimento da guia",
        "guideTotal: o \"Total da Guia (FGTS + Consignado)\"",
        "declaredWorkers: a \"Qtd. Trabalhadores FGTS\"",
        "workers: uma linha por trabalhador da \"Relação de Trabalhadores\", com nome, CPF, base de remuneração e valor do FGTS na guia");

    public string SchemaName => "fgts_detail";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("employerCnpjRoot", VisionPrompt.Nullable("string", "raiz do CNPJ do empregador")),
        ("competence", VisionPrompt.Nullable("string", "competência, MM/yyyy")),
        ("dueDate", VisionPrompt.Nullable("string", "vencimento da guia, dd/MM/yyyy")),
        ("guideTotal", VisionPrompt.Nullable("number", "total da guia, em reais")),
        ("declaredWorkers", VisionPrompt.Nullable("integer", "quantidade de trabalhadores declarada")),
        VisionPrompt.List("workers", "trabalhadores da guia",
            ("name", "string", "nome do trabalhador"),
            ("cpf", "string", "CPF do trabalhador"),
            ("baseAmount", "number", "base de remuneração, em reais"),
            ("fgtsAmount", "number", "valor do FGTS na guia, em reais")));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "FGTS_DETAIL" : null, "identificação como detalhamento do FGTS");
        extraction.Add("employerCnpjRoot", EmployerRoot().Match(flat) is { Success: true } root ? Digits(root.Groups["root"].Value) : null, "raiz do CNPJ do empregador");

        var header = HeaderLabelsFirst().Match(flat);
        extraction.Add("dueDate", header.Success ? header.Groups["due"].Value : DueDate().Match(flat) is { Success: true } due ? due.Groups["date"].Value : null);
        extraction.Add("declaredWorkers", header.Success ? header.Groups["count"].Value : DeclaredWorkers().Match(flat) is { Success: true } count ? count.Groups["count"].Value : null);
        extraction.Add("guideTotal", GuideTotal(flat));
        extraction.Add("tomadorCnpj", Tomador().Match(flat) is { Success: true } tomador ? TextPatterns.ValidCnpj(tomador.Groups["cnpj"].Value) : null);

        var workers = WorkerRow().Matches(flat)
            .Select(row => (Competence: row.Groups["comp"].Value, Worker: new FgtsWorker(
                row.Groups["name"].Value.Trim(),
                TextPatterns.ValidCpf(row.Groups["cpf"].Value) ?? string.Empty,
                Amount(row.Groups["base"].Value),
                Amount(row.Groups["fgts"].Value))))
            .Where(row => row.Worker.Cpf.Length > 0)
            .DistinctBy(row => row.Worker.Cpf)
            .ToList();

        extraction.Add("competence", workers.FirstOrDefault().Competence, "competência");
        extraction.AddList("workers", workers.Select(w => w.Worker).ToList(), "relação de trabalhadores");

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

        var root = Digits(VisionPrompt.GetString(result, "employerCnpjRoot"));

        extraction.Add("documentKind", "FGTS_DETAIL");
        extraction.Add("employerCnpjRoot", root is { Length: 8 } ? root : null, "raiz do CNPJ do empregador");
        extraction.Add("competence", TextPatterns.ParseCompetence(VisionPrompt.GetString(result, "competence")) is { } month ? TextPatterns.FormatCompetence(month) : null, "competência");
        extraction.Add("dueDate", TextPatterns.ParseDate(VisionPrompt.GetString(result, "dueDate")) is { } due ? TextPatterns.FormatDate(due) : null);
        extraction.Add("declaredWorkers", VisionPrompt.GetDecimal(result, "declaredWorkers")?.ToString("0", CultureInfo.InvariantCulture));
        extraction.Add("guideTotal", VisionPrompt.GetDecimal(result, "guideTotal") is { } total ? TextPatterns.FormatAmount(total) : null);

        var workers = VisionPrompt.GetList(result, "workers")
            .Select(item => new FgtsWorker(
                VisionPrompt.GetString(item, "name") ?? string.Empty,
                TextPatterns.ValidCpf(VisionPrompt.GetString(item, "cpf")) ?? string.Empty,
                VisionPrompt.GetDecimal(item, "baseAmount") is { } baseAmount ? TextPatterns.FormatAmount(baseAmount) : null,
                VisionPrompt.GetDecimal(item, "fgtsAmount") is { } fgts ? TextPatterns.FormatAmount(fgts) : null))
            .Where(worker => worker.Cpf.Length > 0)
            .DistinctBy(worker => worker.Cpf)
            .ToList();

        extraction.AddList("workers", workers, "relação de trabalhadores");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        AnalysisChecks.CompanyCnpjRoot(findings, extraction.Get("employerCnpjRoot"), document.Company, Label);
        AnalysisChecks.CompetenceInPeriod(findings, TextPatterns.ParseCompetence(extraction.Get("competence")), document, Label);

        var workers = extraction.GetList<FgtsWorker>("workers");
        if (int.TryParse(extraction.Get("declaredWorkers"), out var declared) && declared != workers.Count)
        {
            findings.Add(new AnalysisFinding(
                "WORKER_COUNT_MISMATCH",
                FindingSeverity.Warning,
                $"A guia declara {declared} trabalhador(es), mas a relação lida tem {workers.Count}."));
        }

        return findings;
    }

    private static string? GuideTotal(string flat)
    {
        var match = GuideTotalBefore().Match(flat);
        if (!match.Success)
        {
            match = GuideTotalAfter().Match(flat);
        }

        return match.Success ? Amount(match.Groups["value"].Value) : null;
    }

    private static string? Amount(string value)
    {
        return TextPatterns.ParseMoney(value) is { } amount ? TextPatterns.FormatAmount(amount) : null;
    }

    private static string? Digits(string? value)
    {
        return value is null ? null : new string(value.Where(char.IsDigit).ToArray());
    }

    [GeneratedRegex(@"Detalhe\s+da\s+Guia|Rela[cç][aã]o\s+de\s+Trabalhadores", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"Empregador:\s*(?<root>\d{2}\.\d{3}\.\d{3})(?!/)")]
    private static partial Regex EmployerRoot();

    [GeneratedRegex(@"Vencimento\s+da\s+Guia:\s*N[uú]mero\s+da\s+Guia:\s*Qtd\.\s*Trabalhadores\s+FGTS:\s*(?<due>\d{2}/\d{2}/\d{4})\s+\S+\s+(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderLabelsFirst();

    [GeneratedRegex(@"Vencimento\s+da\s+Guia:\s*(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex DueDate();

    [GeneratedRegex(@"Qtd\.\s*Trabalhadores\s+FGTS:\s*(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DeclaredWorkers();

    [GeneratedRegex(@"(?<value>[\d.]+,\d{2})\s+Total\s+da\s+Guia", RegexOptions.IgnoreCase)]
    private static partial Regex GuideTotalBefore();

    [GeneratedRegex(@"Total\s+da\s+Guia\s+\(FGTS\s+\+\s+Consignado\):\s*(?<value>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex GuideTotalAfter();

    [GeneratedRegex(@"Tomador:\s*(?<cnpj>\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2})")]
    private static partial Regex Tomador();

    [GeneratedRegex(@"(?<comp>\d{2}/\d{4})\s+(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s+\d{11,}\s+(?<cpf>\d{3}\.\d{3}\.\d{3}-\d{2})\s+\d{3}\s+\d{2}/\d{2}/\d{4}\s+\S+\s+(?<base>[\d.]+,\d{2})\s+(?<fgts>[\d.]+,\d{2})")]
    private static partial Regex WorkerRow();
}
