using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Compliance;

public static class PeriodComplianceRules
{
    public const decimal Tolerance = 0.01m;

    private static readonly string[] CrossedTypes =
    [
        "PAYMENT_RECEIPT", "PAYMENT_PROOF", "TIMESHEET", "FGTS_DETAIL", "FGTS_REPORT", "FGTS_PAYMENT_PROOF", "EMPLOYEE_LIST", "PAYROLL"
    ];

    public static List<AnalysisFinding> Evaluate(PeriodContext context)
    {
        var findings = new List<AnalysisFinding>();
        var documents = context.Documents.Where(d => CrossedTypes.Contains(d.TypeCode)).ToList();

        foreach (var worker in context.AllocatedWorkers)
        {
            WorkerRules(findings, documents, worker);
        }

        FgtsRules(findings, documents, context.AllocatedWorkers);
        EmployeeListRules(findings, documents, context.AllocatedWorkers);
        UnreadDocuments(findings, documents);

        return findings;
    }

    private static void WorkerRules(List<AnalysisFinding> findings, List<PeriodDocument> documents, Worker worker)
    {
        var receipt = Latest(documents, "PAYMENT_RECEIPT", worker.Id);
        var proofs = Read(documents, "PAYMENT_PROOF", worker.Id).Where(p => Amount(p, "amount") is not null).ToList();
        var timesheet = Latest(documents, "TIMESHEET", worker.Id);
        var netPay = receipt is null ? null : Amount(receipt, "netPay");

        if (netPay is { } net && proofs.Count > 0)
        {
            var paid = proofs.Sum(p => Amount(p, "amount")!.Value);
            if (Math.Abs(paid - net) > Tolerance)
            {
                findings.Add(new AnalysisFinding(
                    "PAYMENT_AMOUNT_MISMATCH",
                    Severity([receipt, .. proofs]),
                    $"{worker.Name}: o recibo indica líquido de {TextPatterns.FormatMoney(net)}, mas {Describe(proofs.Count, "o comprovante soma", "os comprovantes somam")} {TextPatterns.FormatMoney(paid)}."));
            }
        }

        if (netPay is > 0 && timesheet is not null && TextPatterns.ParseHours(timesheet.Fields!.Get("workedHours")) == 0)
        {
            findings.Add(new AnalysisFinding(
                "PAID_WITHOUT_WORKED_HOURS",
                FindingSeverity.Warning,
                $"{worker.Name}: a folha de ponto soma 0:00 trabalhadas, mas o recibo paga líquido de {TextPatterns.FormatMoney(netPay.Value)}."));
        }

        PayrollRules(findings, documents, worker, receipt, netPay);

        var fgtsDetail = Latest(documents, "FGTS_DETAIL");
        if (fgtsDetail is not null && !fgtsDetail.Fields!.GetList<FgtsWorker>("workers").Any(w => w.Cpf == worker.Cpf))
        {
            findings.Add(new AnalysisFinding(
                "WORKER_WITHOUT_FGTS",
                Severity([fgtsDetail]),
                $"{worker.Name} está alocado(a) na obra, mas não aparece no detalhamento do FGTS da competência."));
        }
    }

    private static void PayrollRules(List<AnalysisFinding> findings, List<PeriodDocument> documents, Worker worker, PeriodDocument? receipt, decimal? netPay)
    {
        var payroll = Latest(documents, "PAYROLL");
        if (payroll is null)
        {
            return;
        }

        var entry = payroll.Fields!.GetList<PayrollEmployee>("employees")
            .FirstOrDefault(e => TextPatterns.NormalizeName(e.Name) == TextPatterns.NormalizeName(worker.Name));

        if (entry is null)
        {
            findings.Add(new AnalysisFinding(
                "ALLOCATED_NOT_IN_PAYROLL",
                FindingSeverity.Warning,
                $"{worker.Name} está alocado(a) na obra, mas não aparece na folha de pagamento."));
            return;
        }

        if (netPay is { } net && TextPatterns.ParseAmount(entry.NetPay) is { } payrollNet && Math.Abs(payrollNet - net) > Tolerance)
        {
            findings.Add(new AnalysisFinding(
                "PAYROLL_RECEIPT_MISMATCH",
                Severity([receipt, payroll]),
                $"{worker.Name}: o recibo indica líquido de {TextPatterns.FormatMoney(net)}, mas a folha de pagamento indica {TextPatterns.FormatMoney(payrollNet)}."));
        }
    }

    private static void FgtsRules(List<AnalysisFinding> findings, List<PeriodDocument> documents, IReadOnlyList<Worker> allocated)
    {
        var guide = Latest(documents, "FGTS_REPORT");
        var detail = Latest(documents, "FGTS_DETAIL");
        var source = guide ?? detail;
        var proofs = Read(documents, "FGTS_PAYMENT_PROOF").Where(p => Amount(p, "amount") is not null).ToList();

        var guideTotal = Amount(guide, "guideTotal") ?? Amount(detail, "guideTotal");
        if (guideTotal is { } total && proofs.Count > 0)
        {
            var paid = proofs.Sum(p => Amount(p, "amount")!.Value);
            if (Math.Abs(paid - total) > Tolerance)
            {
                findings.Add(new AnalysisFinding(
                    "FGTS_PAYMENT_MISMATCH",
                    Severity([source, .. proofs]),
                    $"A guia do FGTS é de {TextPatterns.FormatMoney(total)}, mas {Describe(proofs.Count, "o comprovante soma", "os comprovantes somam")} {TextPatterns.FormatMoney(paid)}."));
            }
        }

        var dueDate = TextPatterns.ParseDate(guide?.Fields!.Get("dueDate")) ?? TextPatterns.ParseDate(detail?.Fields!.Get("dueDate"));
        var latePayment = dueDate is null
            ? null
            : proofs.Select(p => TextPatterns.ParseDate(p.Fields!.Get("paymentDate"))).Where(d => d > dueDate).Max();
        if (latePayment is { } paidAt)
        {
            findings.Add(new AnalysisFinding(
                "FGTS_PAID_LATE",
                FindingSeverity.Warning,
                $"O FGTS foi pago em {TextPatterns.FormatDate(paidAt)}, depois do vencimento da guia ({TextPatterns.FormatDate(dueDate!.Value)})."));
        }

        var declared = int.TryParse(guide?.Fields!.Get("declaredWorkers") ?? detail?.Fields!.Get("declaredWorkers"), out var count) ? count : (int?)null;
        if (declared is { } workers && workers < allocated.Count)
        {
            findings.Add(new AnalysisFinding(
                "FGTS_FEWER_WORKERS_THAN_ALLOCATED",
                FindingSeverity.Warning,
                $"A guia do FGTS declara {workers} trabalhador(es), mas a obra tem {allocated.Count} alocado(s) no período."));
        }
    }

    private static void EmployeeListRules(List<AnalysisFinding> findings, List<PeriodDocument> documents, IReadOnlyList<Worker> allocated)
    {
        var list = Latest(documents, "EMPLOYEE_LIST");
        if (list is null)
        {
            return;
        }

        var listed = list.Fields!.GetList<ListedEmployee>("employees");

        foreach (var worker in allocated.Where(w => !listed.Any(e => Matches(e, w))))
        {
            findings.Add(new AnalysisFinding(
                "ALLOCATED_NOT_IN_EMPLOYEE_LIST",
                FindingSeverity.Warning,
                $"{worker.Name} está alocado(a) na obra, mas não aparece na relação de funcionários."));
        }

        foreach (var employee in listed.Where(e => !allocated.Any(w => Matches(e, w))))
        {
            findings.Add(new AnalysisFinding(
                "LISTED_NOT_ALLOCATED",
                FindingSeverity.Warning,
                $"{employee.Name} aparece na relação de funcionários, mas não está alocado(a) na obra pelo portal."));
        }
    }

    private static void UnreadDocuments(List<AnalysisFinding> findings, List<PeriodDocument> documents)
    {
        var unread = documents
            .Where(d => !d.IsRead)
            .Select(d => d.Document.Worker is null ? d.Document.DocumentType.Name : $"{d.Document.DocumentType.Name} ({d.Document.Worker.Name})")
            .Distinct()
            .ToList();

        if (unread.Count > 0)
        {
            findings.Add(new AnalysisFinding(
                "UNREAD_DOCUMENTS",
                FindingSeverity.Info,
                $"Ainda sem leitura, fora do cruzamento: {string.Join("; ", unread)}."));
        }
    }

    private static bool Matches(ListedEmployee employee, Worker worker)
    {
        return employee.Cpf is not null
            ? employee.Cpf == worker.Cpf
            : TextPatterns.NormalizeName(employee.Name) == TextPatterns.NormalizeName(worker.Name);
    }

    private static PeriodDocument? Latest(List<PeriodDocument> documents, string typeCode, long? workerId = null)
    {
        return Read(documents, typeCode, workerId).FirstOrDefault();
    }

    private static IEnumerable<PeriodDocument> Read(List<PeriodDocument> documents, string typeCode, long? workerId = null)
    {
        return documents
            .Where(d => d.IsRead && d.TypeCode == typeCode && (workerId is null || d.Document.WorkerId == workerId))
            .OrderByDescending(d => d.Document.CreatedAt);
    }

    private static decimal? Amount(PeriodDocument? document, string field)
    {
        return document?.Fields is null ? null : TextPatterns.ParseAmount(document.Fields.Get(field));
    }

    private static FindingSeverity Severity(IEnumerable<PeriodDocument?> sources)
    {
        return sources.All(s => s is { ReadByVision: false }) ? FindingSeverity.Blocking : FindingSeverity.Warning;
    }

    private static string Describe(int count, string singular, string plural)
    {
        return count == 1 ? singular : $"{plural} ({count})";
    }
}
