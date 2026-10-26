using System.Text.Json;
using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public partial class FgtsPaymentProofAnalyzer : IVisionAnalyzer
{
    public const string CaixaCnpjRoot = "00360305";

    public IReadOnlyCollection<string> DocumentTypeCodes { get; } = ["FGTS_PAYMENT_PROOF"];

    public string Instructions { get; } = VisionPrompt.Instructions(
        "comprovante de pagamento da guia do FGTS (FGTS Digital, pago por PIX ou boleto à Caixa Econômica Federal)",
        "payerDocument: o CPF ou CNPJ de quem pagou",
        "receiverDocument: o CPF ou CNPJ de quem recebeu (normalmente a Caixa)",
        "amount: o valor efetivamente pago (\"valor da transação\" ou \"valor pago\")",
        "paymentDate: a data em que o pagamento foi efetuado");

    public string SchemaName => "fgts_payment_proof";

    public string JsonSchema { get; } = VisionPrompt.Schema(
        ("payerDocument", "string", "CPF ou CNPJ de quem pagou"),
        ("receiverDocument", "string", "CPF ou CNPJ de quem recebeu"),
        ("amount", "number", "valor pago, em reais"),
        ("paymentDate", "string", "data do pagamento, dd/MM/yyyy"));

    public FieldExtraction Extract(DocumentText text)
    {
        var extraction = new FieldExtraction();
        var flat = text.Flat;

        extraction.Add("documentKind", Title().IsMatch(flat) ? "FGTS_PAYMENT_PROOF" : null, "identificação como comprovante de pagamento");
        extraction.Add("payerCnpj", Payer().Match(flat) is { Success: true } payer ? TextPatterns.ValidCnpj(payer.Groups["cnpj"].Value) : null, "CNPJ de quem pagou");
        extraction.Add("receiverCnpj", Receiver().Match(flat) is { Success: true } receiver ? TextPatterns.ValidCnpj(receiver.Groups["cnpj"].Value) : null);
        extraction.Add("amount", PaidAmount(flat), "valor pago");
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

        extraction.Add("documentKind", "FGTS_PAYMENT_PROOF");
        extraction.Add("payerCnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "payerDocument")), "CNPJ de quem pagou");
        extraction.Add("receiverCnpj", TextPatterns.ValidCnpj(VisionPrompt.GetString(result, "receiverDocument")));
        extraction.Add("amount", VisionPrompt.GetDecimal(result, "amount") is { } amount ? TextPatterns.FormatAmount(amount) : null, "valor pago");
        extraction.Add("paymentDate", ValidDate(VisionPrompt.GetString(result, "paymentDate")), "data do pagamento");

        return extraction;
    }

    public IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today)
    {
        var findings = new List<AnalysisFinding>();

        var payerCnpj = extraction.Get("payerCnpj")!;
        if (!payerCnpj.StartsWith(document.Company.Cnpj[..8], StringComparison.Ordinal))
        {
            findings.Add(new AnalysisFinding(
                "PAYER_MISMATCH",
                FindingSeverity.Warning,
                $"O FGTS foi pago pelo CNPJ {Cnpj.Format(payerCnpj)}, e não pela empresa cadastrada ({Cnpj.Format(document.Company.Cnpj)})."));
        }

        var receiverCnpj = extraction.Get("receiverCnpj");
        if (receiverCnpj is not null && !receiverCnpj.StartsWith(CaixaCnpjRoot, StringComparison.Ordinal))
        {
            findings.Add(new AnalysisFinding(
                "RECEIVER_NOT_CAIXA",
                FindingSeverity.Warning,
                $"O pagamento foi feito para o CNPJ {Cnpj.Format(receiverCnpj)}, e não para a Caixa Econômica Federal."));
        }

        return findings;
    }

    private static string? PaidAmount(string flat)
    {
        var match = TransactionAmount().Match(flat);
        if (!match.Success)
        {
            match = DocumentAmount().Match(flat);
        }

        return match.Success && TextPatterns.ParseMoney(match.Groups["value"].Value) is { } amount ? TextPatterns.FormatAmount(amount) : null;
    }

    private static string? ValidDate(string? value)
    {
        return TextPatterns.ParseDate(value) is { } date ? TextPatterns.FormatDate(date) : null;
    }

    [GeneratedRegex(@"comprovante\s+de\s+pagamento", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    [GeneratedRegex(@"CPF/CNPJ\s+do\s+pagador:?\s*(?<cnpj>\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Payer();

    [GeneratedRegex(@"CPF/CNPJ\s+do\s+recebedor:?\s*(?<cnpj>\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Receiver();

    [GeneratedRegex(@"valor\s+(?:da\s+transa[cç][aã]o|pago):?\s*R\$\s*(?<value>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TransactionAmount();

    [GeneratedRegex(@"valor\s+do\s+documento:?\s*R\$\s*(?<value>[\d.]+,\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentAmount();

    [GeneratedRegex(@"(?:pagamento\s+efetuado\s+em|data\s+do\s+pagamento|efetuad[ao]\s+em):?\s*(?<date>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentDate();
}
