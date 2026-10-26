using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public partial class PaymentProofAnalyzer : IVisionAnalyzer
{
    private const string Label = "comprovante";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["PAYMENT_PROOF"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "comprovante de pagamento de salário a um trabalhador (transferência, PIX, TED ou depósito)",
        "payerName e payerDocument: quem pagou e o CPF ou CNPJ dele",
        "payeeName e payeeDocument: quem recebeu e o CPF dele. Se o CPF estiver parcialmente oculto, transcreva com os asteriscos (ex.: ***.952.844-**)",
        "amount: o valor pago",
        "paymentDate: a data em que o pagamento foi efetuado");

    public string SchemaName => "payment_proof";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("payerName", "string", "nome de quem pagou"),
        ("payerDocument", "string", "CPF ou CNPJ de quem pagou"),
        ("payeeName", "string", "nome de quem recebeu"),
        ("payeeDocument", "string", "CPF de quem recebeu, com asteriscos se oculto"),
        ("amount", "number", "valor pago, em reais"),
        ("paymentDate", "string", "data do pagamento, dd/MM/yyyy"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "PAYMENT_PROOF" : null, "identificação como comprovante de pagamento");

        var payeeStart = PayeeSection().Match(flat);
        var payerPart = payeeStart.Success ? flat[..payeeStart.Index] : flat;
        var payeePart = payeeStart.Success ? flat[payeeStart.Index..] : flat;

        extraction.Add("payerCnpj", TextPatterns.FirstValidCnpj(payerPart));
        extraction.Add("payeeName", PayeeName().Match(payeePart) is { Success: true } name ? name.Groups["name"].Value : null);
        extraction.Add("payeeCpf", PayeeCpf(payeePart), "CPF de quem recebeu");
        extraction.Add("amount", Amount().Match(flat) is { Success: true } amount ? FormatMoney(amount.Groups["value"].Value) : null, "valor pago");
        extraction.Add("paymentDate", PaymentDate().Match(flat) is { Success: true } date ? ValidDate(date.Groups["date"].Value) : null, "data do pagamento");

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

        var payeeDocument = VisionPrompt.GetString(result, "payeeDocument");

        extraction.Add("documentKind", "PAYMENT_PROOF");
        extraction.Add("payerCnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "payerDocument")));
        extraction.Add("payeeName", VisionPrompt.GetString(result, "payeeName"));
        extraction.Add("payeeCpf", NormalizeCpf(payeeDocument), "CPF de quem recebeu");
        extraction.Add("amount", VisionPrompt.GetDecimal(result, "amount") is { } amount ? TextPatterns.FormatAmount(amount) : null, "valor pago");
        extraction.Add("paymentDate", ValidDate(VisionPrompt.GetString(result, "paymentDate")), "data do pagamento");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        var payerCnpj = extraction.Get("payerCnpj");
        if (payerCnpj is not null && payerCnpj != document.Company.Cnpj)
        {
            findings.Add(new AnalysisFinding(
                "PAYER_MISMATCH",
                FindingSeverity.Warning,
                $"O pagamento foi feito pelo CNPJ {Cnpj.Format(payerCnpj)}, e não pela empresa cadastrada ({Cnpj.Format(document.Company.Cnpj)})."));
        }

        AnalysisChecks.WorkerCpf(findings, extraction.Get("payeeCpf"), document.Worker, Label);
        AnalysisChecks.WorkerName(findings, extraction.Get("payeeName"), document.Worker, Label);

        if (document.ReferencePeriodStart is { } periodStart && TextPatterns.ParseDate(extraction.Get("paymentDate")) is { } paidAt)
        {
            var deadline = SalaryCalendar.FifthBusinessDayAfter(periodStart);
            if (paidAt > deadline)
            {
                findings.Add(new AnalysisFinding(
                    "LATE_SALARY_PAYMENT",
                    FindingSeverity.Warning,
                    $"Salário de {TextPatterns.FormatCompetence(periodStart)} pago em {TextPatterns.FormatDate(paidAt)}, depois do 5º dia útil ({TextPatterns.FormatDate(deadline)})."));
            }
        }

        return findings;
    }

    private static string? PayeeCpf(string payeePart)
    {
        var match = CpfOrMasked().Match(payeePart);
        return match.Success ? NormalizeCpf(match.Value) : null;
    }

    private static string? NormalizeCpf(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Contains('*') ? TextPatterns.NormalizeMaskedCpf(value) : TextPatterns.ValidCpf(value);
    }

    private static string? FormatMoney(string value)
    {
        return TextPatterns.ParseMoney(value) is { } amount ? TextPatterns.FormatAmount(amount) : null;
    }

    private static string? ValidDate(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    [GeneratedRegex(@"comprovante\s+de\s+(transfer[eê]ncia|pagamento|pix|ted|dep[oó]sito)", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"(quem\s+est[aá]\s+recebendo|dados\s+do\s+recebedor|destinat[aá]rio|favorecido)", RegexOptions.IgnoreCase)]
    private static partial Regex PayeeSection();

    [GeneratedRegex(@"Nome:?\s+(?<name>[A-ZÀ-Ü][A-ZÀ-Ü' ]+?)\s+(CPF|Chave|Institui)", RegexOptions.IgnoreCase)]
    private static partial Regex PayeeName();

    [GeneratedRegex(@"[\d*]{3}\.[\d*]{3}\.[\d*]{3}-[\d*]{2}")]
    private static partial Regex CpfOrMasked();

    [GeneratedRegex(@"\bValor(?:\s+(?:da\s+transa[cç][aã]o|pago|do\s+pagamento|transferido))?:?\s*R\$\s*(?<value>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Amount();

    [GeneratedRegex(@"(?:Data\s+da\s+transfer[eê]ncia|Data\s+do\s+pagamento|Efetuad[ao]\s+em|Realizad[ao]\s+em|pagamento\s+efetuado\s+em):?\s*(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentDate();
}
