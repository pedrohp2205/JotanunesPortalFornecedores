using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Application.Services.Compliance;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Compliance;

public class PeriodComplianceRulesTests
{
    private static readonly Worker Maria = new(1, AnalysisTestData.WorkerName, AnalysisTestData.WorkerCpf) { Id = 7 };
    private static readonly Worker Joao = new(1, "Joao Pereira Lima", "111.444.777-35") { Id = 8 };

    private readonly List<PeriodDocument> _documents = [];
    private int _sequence;

    private PeriodDocument Add(
        string typeCode,
        Worker? worker = null,
        TextExtractionEngine? engine = TextExtractionEngine.NativeText,
        params (string Name, string Value)[] fields)
    {
        var subject = worker is null ? DocumentSubject.Company : DocumentSubject.Worker;
        var document = AnalysisTestData.RecurringDocument(typeCode, typeCode switch
        {
            "PAYMENT_RECEIPT" => "Recibo de Pagamento",
            "PAYMENT_PROOF" => "Comprovante de Pagamento",
            "TIMESHEET" => "Folha de Ponto",
            _ => typeCode
        }, subject, withWorker: false);
        document.CreatedAt = new DateTime(2026, 8, 1).AddMinutes(++_sequence);
        if (worker is not null)
        {
            typeof(Document).GetProperty(nameof(Document.WorkerId))!.SetValue(document, worker.Id);
            typeof(Document).GetProperty(nameof(Document.Worker))!.SetValue(document, worker);
        }

        var read = engine is null ? null : FieldExtraction.FromFields(fields.Select(f => new ExtractedField(f.Name, f.Value)));
        var periodDocument = new PeriodDocument(document, read, engine);
        _documents.Add(periodDocument);
        return periodDocument;
    }

    private List<AnalysisFinding> Evaluate(params Worker[] allocated)
    {
        return PeriodComplianceRules.Evaluate(new PeriodContext(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), _documents, allocated));
    }

    [Fact]
    public void Should_Find_Nothing_When_Receipt_Proof_And_Timesheet_Agree()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.Vision, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "1900.75"));
        Add("TIMESHEET", Maria, TextExtractionEngine.NativeText, ("workedHours", "168:37"));

        Assert.Empty(Evaluate(Maria));
    }

    [Fact]
    public void Should_Flag_Payment_Different_From_Receipt()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.NativeText, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "1500.00"));

        var finding = Assert.Single(Evaluate(Maria));

        Assert.Equal("PAYMENT_AMOUNT_MISMATCH", finding.Code);
        Assert.Equal(FindingSeverity.Blocking, finding.Severity);
        Assert.Contains("R$ 1.900,75", finding.Message);
        Assert.Contains("R$ 1.500,00", finding.Message);
    }

    [Fact]
    public void Should_Only_Warn_About_Payment_Mismatch_When_A_Side_Was_Read_By_Vision()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.Vision, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "1500.00"));

        Assert.Equal(FindingSeverity.Warning, Assert.Single(Evaluate(Maria)).Severity);
    }

    [Fact]
    public void Should_Sum_Split_Payments()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.NativeText, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "900.00"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "1000.75"));

        Assert.Empty(Evaluate(Maria));
    }

    [Fact]
    public void Should_Use_The_Latest_Receipt()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.NativeText, ("netPay", "1500.00"));
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.NativeText, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Maria, TextExtractionEngine.NativeText, ("amount", "1900.75"));

        Assert.Empty(Evaluate(Maria));
    }

    [Fact]
    public void Should_Not_Mix_Documents_Of_Different_Workers()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.NativeText, ("netPay", "1900.75"));
        Add("PAYMENT_PROOF", Joao, TextExtractionEngine.NativeText, ("amount", "1500.00"));

        Assert.Empty(Evaluate(Maria, Joao));
    }

    [Fact]
    public void Should_Warn_When_Salary_Is_Paid_Without_Worked_Hours()
    {
        Add("PAYMENT_RECEIPT", Maria, TextExtractionEngine.Vision, ("netPay", "1900.75"));
        Add("TIMESHEET", Maria, TextExtractionEngine.NativeText, ("workedHours", "0:00"));

        var finding = Assert.Single(Evaluate(Maria));

        Assert.Equal("PAID_WITHOUT_WORKED_HOURS", finding.Code);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void Should_Flag_Allocated_Worker_Missing_From_Fgts_Detail()
    {
        Add("FGTS_DETAIL", engine: TextExtractionEngine.NativeText, fields: ("workers", """[{"name":"MARIA APARECIDA DOS SANTOS","cpf":"52998224725"}]"""));

        var finding = Assert.Single(Evaluate(Maria, Joao));

        Assert.Equal("WORKER_WITHOUT_FGTS", finding.Code);
        Assert.Equal(FindingSeverity.Blocking, finding.Severity);
        Assert.Contains("Joao Pereira Lima", finding.Message);
    }

    [Fact]
    public void Should_Flag_Fgts_Payment_Different_From_Guide_Paid_Late_And_Declaring_Fewer_Workers()
    {
        Add("FGTS_REPORT", engine: TextExtractionEngine.Vision, fields: [("guideTotal", "2421.15"), ("dueDate", "20/08/2026"), ("declaredWorkers", "1")]);
        Add("FGTS_PAYMENT_PROOF", engine: TextExtractionEngine.NativeText, fields: [("amount", "2400.00"), ("paymentDate", "21/08/2026")]);

        var findings = Evaluate(Maria, Joao);

        Assert.Contains(findings, f => f.Code == "FGTS_PAYMENT_MISMATCH" && f.Severity == FindingSeverity.Warning);
        Assert.Contains(findings, f => f.Code == "FGTS_PAID_LATE" && f.Message.Contains("21/08/2026"));
        Assert.Contains(findings, f => f.Code == "FGTS_FEWER_WORKERS_THAN_ALLOCATED");
    }

    [Fact]
    public void Should_Use_Fgts_Detail_When_There_Is_No_Guide()
    {
        Add("FGTS_DETAIL", engine: TextExtractionEngine.NativeText, fields:
        [
            ("guideTotal", "2421.15"), ("dueDate", "20/08/2026"), ("declaredWorkers", "14"),
            ("workers", """[{"name":"MARIA APARECIDA DOS SANTOS","cpf":"52998224725"}]""")
        ]);
        Add("FGTS_PAYMENT_PROOF", engine: TextExtractionEngine.NativeText, fields: [("amount", "2421.15"), ("paymentDate", "08/08/2026")]);

        Assert.Empty(Evaluate(Maria));
    }

    [Fact]
    public void Should_Compare_Employee_List_With_Allocations_By_Cpf_Or_Name()
    {
        Add("EMPLOYEE_LIST", engine: TextExtractionEngine.NativeText, fields: ("employees",
            """[{"name":"MARIA APARECIDA DOS SANTOS","cpf":null},{"name":"OUTRO NOME","cpf":"11144477735"},{"name":"CARLOS SILVA","cpf":null}]"""));
        var allocatedOnly = new Worker(1, "Ana Paula Souza", "390.533.447-05") { Id = 9 };

        var findings = Evaluate(Maria, Joao, allocatedOnly);

        Assert.Equal(
            ["ALLOCATED_NOT_IN_EMPLOYEE_LIST", "LISTED_NOT_ALLOCATED"],
            findings.Select(f => f.Code).Order().ToArray());
        Assert.Contains(findings, f => f.Message.Contains("Ana Paula Souza"));
        Assert.Contains(findings, f => f.Message.Contains("CARLOS SILVA"));
    }

    [Fact]
    public void Should_List_Documents_Not_Read_Yet()
    {
        Add("PAYMENT_RECEIPT", Maria, engine: null);
        Add("FGTS_REPORT", engine: null);
        Add("CNPJ_CARD", engine: null);

        var finding = Assert.Single(Evaluate(Maria));

        Assert.Equal("UNREAD_DOCUMENTS", finding.Code);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Contains("Recibo de Pagamento (Maria Aparecida dos Santos)", finding.Message);
        Assert.Contains("FGTS_REPORT", finding.Message);
        Assert.DoesNotContain("CNPJ_CARD", finding.Message);
    }
}
