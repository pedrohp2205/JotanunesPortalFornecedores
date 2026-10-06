using System.Globalization;
using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public class PaymentReceiptAnalyzer : IVisionAnalyzer
{
    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["PAYMENT_RECEIPT"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "recibo de pagamento de salário (holerite, contracheque ou recibo da folha de pagamento)",
        "employerName e employerCnpj: o empregador e o CNPJ (\"Inscrição\")",
        "employeeName e employeeCpf: o empregado e o CPF dele",
        "competence: o mês de referência, em MM/yyyy (ex.: \"Julho de 2026\" vira 07/2026)",
        "netPay: o \"Líquido a Receber\"",
        "employeeSigned: true se há assinatura manuscrita do empregado no campo de assinatura, false se o campo está em branco, null se não houver campo de assinatura. Se houver mais de uma via, considere true se qualquer via estiver assinada");

    public string SchemaName => "payment_receipt";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("employerName", "string", "nome do empregador"),
        ("employerCnpj", "string", "CNPJ do empregador"),
        ("employeeName", "string", "nome do empregado"),
        ("employeeCpf", "string", "CPF do empregado"),
        ("competence", "string", "competência, MM/yyyy"),
        ("netPay", "number", "líquido a receber, em reais"),
        ("employeeSigned", "boolean", "se o empregado assinou o recibo"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        extraction.Add("documentKind", null, "leitura do recibo por imagem");
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

        extraction.Add("documentKind", "PAYMENT_RECEIPT");
        extraction.Add("employeeCpf", TextPatterns.ValidCpf(VisionPrompt.GetString(result, "employeeCpf")), "CPF do empregado");
        extraction.Add("competence", TextPatterns.ParseCompetence(VisionPrompt.GetString(result, "competence")) is { } month
            ? month.ToString("MM/yyyy", CultureInfo.InvariantCulture)
            : null, "competência");
        extraction.Add("netPay", VisionPrompt.GetDecimal(result, "netPay")?.ToString(CultureInfo.InvariantCulture), "líquido a receber");
        extraction.Add("employeeName", VisionPrompt.GetString(result, "employeeName"));
        extraction.Add("employerName", VisionPrompt.GetString(result, "employerName"));
        extraction.Add("employerCnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "employerCnpj")));
        extraction.Add("employeeSigned", VisionPrompt.GetBool(result, "employeeSigned") is { } signed ? (signed ? "true" : "false") : null);

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        var employerCnpj = extraction.Get("employerCnpj");
        if (employerCnpj is not null && employerCnpj != document.Company.Cnpj)
        {
            findings.Add(new AnalysisFinding(
                "EMPLOYER_CNPJ_MISMATCH",
                FindingSeverity.Blocking,
                $"O recibo é do empregador {Cnpj.Format(employerCnpj)}, mas a empresa cadastrada é {Cnpj.Format(document.Company.Cnpj)}."));
        }

        AddWorkerFindings(findings, extraction, document.Worker);
        AddCompetenceFindings(findings, extraction, document);

        if (extraction.Get("employeeSigned") == "false")
        {
            findings.Add(new AnalysisFinding(
                "NOT_SIGNED",
                FindingSeverity.Warning,
                "O recibo não tem a assinatura do empregado."));
        }

        return findings;
    }

    private static void AddWorkerFindings(List<AnalysisFinding> findings, FieldExtraction extraction, Worker? worker)
    {
        if (worker is null)
        {
            return;
        }

        var cpf = extraction.Get("employeeCpf")!;
        if (cpf != worker.Cpf)
        {
            findings.Add(new AnalysisFinding(
                "WORKER_CPF_MISMATCH",
                FindingSeverity.Blocking,
                $"O recibo é do CPF {Cpf.Format(cpf)}, mas o documento foi enviado para {worker.Name} ({Cpf.Format(worker.Cpf)})."));
        }

        var name = extraction.Get("employeeName");
        if (name is not null && TextPatterns.NormalizeName(name) != TextPatterns.NormalizeName(worker.Name))
        {
            findings.Add(new AnalysisFinding(
                "WORKER_NAME_MISMATCH",
                FindingSeverity.Warning,
                $"Nome no recibo (\"{name}\") difere do trabalhador cadastrado (\"{worker.Name}\")."));
        }
    }

    private static void AddCompetenceFindings(List<AnalysisFinding> findings, FieldExtraction extraction, Document document)
    {
        if (document.ReferencePeriodStart is not { } periodStart || document.ReferencePeriodEnd is not { } periodEnd)
        {
            return;
        }

        var competenceStart = TextPatterns.ParseCompetence(extraction.Get("competence"))!.Value;
        var competenceEnd = competenceStart.AddMonths(1).AddDays(-1);

        if (competenceStart > periodEnd || competenceEnd < periodStart)
        {
            findings.Add(new AnalysisFinding(
                "COMPETENCE_OUT_OF_PERIOD",
                FindingSeverity.Blocking,
                $"A competência do recibo ({extraction.Get("competence")}) está fora do período do envio ({TextPatterns.FormatDate(periodStart)} a {TextPatterns.FormatDate(periodEnd)})."));
        }
    }
}
