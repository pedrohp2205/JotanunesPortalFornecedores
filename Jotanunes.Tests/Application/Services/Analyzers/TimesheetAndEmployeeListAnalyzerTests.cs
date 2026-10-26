using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class TimesheetAndEmployeeListAnalyzerTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);
    private readonly TimesheetAnalyzer _timesheet = new();
    private readonly EmployeeListAnalyzer _employeeList = new();

    private static string TimesheetText(
        string cpf = "52998224725",
        string employer = "CNPJ: null",
        string worked = "168:37",
        string period = "01/07/2026 a 31/07/2026",
        string local = "RESIDENCIAL AURORA - BLOCO B")
    {
        return $"DADOS DO EMPREGADOR Nome: Endereço: {employer} {period} Folha de Ponto Local: {local} Nome: DADOS DO COLABORADOR MARIA APARECIDA DOS SANTOS Função: porteiro CPF: {cpf} Admissão: 06/09/2023 -quarta-feira01/07 17:54 05:46 |sexta-feira31/07 {worked} 180:00 MARIA APARECIDA DOS SANTOS CONSTRUTORA EXEMPLO";
    }

    private static Jotanunes.Domain.Entities.Document WorkerDocument() =>
        AnalysisTestData.RecurringDocument("TIMESHEET", "Folha de Ponto", DocumentSubject.Worker);

    [Fact]
    public void Should_Read_Timesheet_Identity_Period_And_Totals()
    {
        var extraction = _timesheet.Extract(new DocumentText([TimesheetText()]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("MARIA APARECIDA DOS SANTOS", extraction.Get("employeeName"));
        Assert.Equal("52998224725", extraction.Get("employeeCpf"));
        Assert.Equal("01/07/2026", extraction.Get("periodStart"));
        Assert.Equal("31/07/2026", extraction.Get("periodEnd"));
        Assert.Equal("168:37", extraction.Get("workedHours"));
        Assert.Equal("180:00", extraction.Get("expectedHours"));
        Assert.Equal("true", extraction.Get("employerCnpjMissing"));
    }

    [Fact]
    public void Should_Only_Warn_About_Missing_Employer_Cnpj_For_A_Regular_Timesheet()
    {
        var findings = _timesheet.Validate(_timesheet.Extract(new DocumentText([TimesheetText()])), WorkerDocument(), Today);

        Assert.Equal("MISSING_EMPLOYER_CNPJ", Assert.Single(findings).Code);
    }

    [Fact]
    public void Should_Warn_When_No_Hours_Were_Worked()
    {
        var findings = _timesheet.Validate(_timesheet.Extract(new DocumentText([TimesheetText(worked: "0:00")])), WorkerDocument(), Today);

        var finding = Assert.Single(findings, f => f.Code == "NO_WORKED_HOURS");
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("180:00", finding.Message);
    }

    [Fact]
    public void Should_Block_Timesheet_Of_Another_Worker_Company_Or_Period()
    {
        var text = TimesheetText(cpf: "11144477735", employer: "CNPJ: 11.444.777/0001-61", period: "01/06/2026 a 30/06/2026");

        var findings = _timesheet.Validate(_timesheet.Extract(new DocumentText([text])), WorkerDocument(), Today);

        Assert.Contains(findings, f => f.Code == "WORKER_CPF_MISMATCH" && f.Severity == FindingSeverity.Blocking);
        Assert.Contains(findings, f => f.Code == "CNPJ_MISMATCH" && f.Severity == FindingSeverity.Blocking);
        Assert.Contains(findings, f => f.Code == "PERIOD_OUT_OF_RANGE" && f.Severity == FindingSeverity.Blocking);
    }

    [Fact]
    public void Should_Warn_When_Timesheet_Workplace_Is_Another_Work_Site()
    {
        var findings = _timesheet.Validate(_timesheet.Extract(new DocumentText([TimesheetText(local: "JOTA NUNES - SUNVILLE")])), WorkerDocument(), Today);

        Assert.Contains(findings, f => f.Code == "WORKPLACE_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Read_One_Employee_Per_Line()
    {
        var extraction = _employeeList.Extract(new DocumentText(["""
            RELAÇÃO DOS FUNCIONARIOS LOTADOS NA OBRA
            NOME DA OBRA : RESIDENCIAL AURORA

            MARIA APARECIDA DOS SANTOS
            JOAO PEREIRA LIMA - CPF 111.444.777-35
            """]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("RESIDENCIAL AURORA", extraction.Get("workSiteName"));
        var employees = extraction.GetList<ListedEmployee>("employees");
        Assert.Equal(
            [new ListedEmployee("MARIA APARECIDA DOS SANTOS", null), new ListedEmployee("JOAO PEREIRA LIMA", "11144477735")],
            employees);
    }

    [Theory]
    [InlineData("RESIDENCIAL AURORA", false)]
    [InlineData("Aurora", false)]
    [InlineData("UNIQUE", true)]
    public void Should_Warn_When_List_Is_From_Another_Work_Site(string listedSite, bool warns)
    {
        var extraction = _employeeList.Extract(new DocumentText([$"RELAÇÃO DOS FUNCIONARIOS LOTADOS NA OBRA\nNOME DA OBRA : {listedSite}\nMARIA APARECIDA DOS SANTOS"]));

        var findings = _employeeList.Validate(extraction, AnalysisTestData.RecurringDocument("EMPLOYEE_LIST", "Relação", DocumentSubject.Company), Today);

        Assert.Equal(warns, findings.Any(f => f.Code == "WORK_SITE_MISMATCH"));
    }
}
