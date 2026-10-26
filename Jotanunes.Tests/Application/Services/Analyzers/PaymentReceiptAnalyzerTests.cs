using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class PaymentReceiptAnalyzerTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);
    private readonly PaymentReceiptAnalyzer _analyzer = new();

    private FieldExtraction FromVision(string json)
    {
        using var document = JsonDocument.Parse(json);
        return _analyzer.FromVision(document.RootElement);
    }

    private List<AnalysisFinding> Validate(string json, Document? document = null)
    {
        var extraction = FromVision(json);
        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        return _analyzer.Validate(extraction, document ?? AnalysisTestData.ReceiptDocument(), Today).ToList();
    }

    [Fact]
    public void Should_Map_Vision_Result_To_Normalized_Fields()
    {
        var extraction = FromVision(AnalysisTestData.ReceiptJson());

        Assert.True(extraction.IsComplete);
        Assert.Equal("52998224725", extraction.Get("employeeCpf"));
        Assert.Equal("11222333000181", extraction.Get("employerCnpj"));
        Assert.Equal("07/2026", extraction.Get("competence"));
        Assert.Equal("1900.75", extraction.Get("netPay"));
        Assert.Equal("false", extraction.Get("employeeSigned"));
    }

    [Fact]
    public void Should_Treat_Cpf_With_Wrong_Check_Digit_As_Not_Read()
    {
        var extraction = FromVision(AnalysisTestData.ReceiptJson(employeeCpf: "529.982.247-26"));

        Assert.False(extraction.IsComplete);
        Assert.Contains("CPF do empregado", extraction.Missing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Julho de 2026")]
    [InlineData("13/2026")]
    public void Should_Require_Competence_As_Month_And_Year(string? competence)
    {
        var extraction = FromVision(AnalysisTestData.ReceiptJson(competence: competence));

        Assert.Contains("competência", extraction.Missing);
    }

    [Fact]
    public void Should_Require_Net_Pay()
    {
        var extraction = FromVision(AnalysisTestData.ReceiptJson(netPay: "null"));

        Assert.Contains("líquido a receber", extraction.Missing);
    }

    [Fact]
    public void Should_Flag_Other_Documents()
    {
        var extraction = FromVision(AnalysisTestData.ReceiptJson(isExpectedDocument: false, detectedDocument: "comprovante de transferência PIX"));

        Assert.False(extraction.IsComplete);
        Assert.Equal("comprovante de transferência PIX", extraction.WrongDocument);
    }

    [Fact]
    public void Should_Not_Read_Anything_From_Text_Layer()
    {
        var extraction = _analyzer.Extract(new DocumentText(["Recibo de Pagamento CPF 529.982.247-25"]));

        Assert.False(extraction.IsComplete);
    }

    [Fact]
    public void Should_Only_Warn_About_Missing_Signature_For_A_Matching_Receipt()
    {
        var finding = Assert.Single(Validate(AnalysisTestData.ReceiptJson()));

        Assert.Equal("NOT_SIGNED", finding.Code);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void Should_Report_Nothing_For_A_Signed_Matching_Receipt()
    {
        Assert.Empty(Validate(AnalysisTestData.ReceiptJson(employeeSigned: "true")));
    }

    [Fact]
    public void Should_Block_Receipt_Of_Another_Worker()
    {
        var findings = Validate(AnalysisTestData.ReceiptJson(employeeCpf: "111.444.777-35", employeeSigned: "true"));

        Assert.Contains(findings, f => f.Code == "WORKER_CPF_MISMATCH" && f.Severity == FindingSeverity.Blocking);
    }

    [Fact]
    public void Should_Block_Receipt_From_Another_Employer()
    {
        var findings = Validate(AnalysisTestData.ReceiptJson(employerCnpj: "11.444.777/0001-61", employeeSigned: "true"));

        Assert.Contains(findings, f => f.Code == "EMPLOYER_CNPJ_MISMATCH" && f.Severity == FindingSeverity.Blocking);
    }

    [Fact]
    public void Should_Ignore_Accents_When_Comparing_Worker_Name()
    {
        var findings = Validate(AnalysisTestData.ReceiptJson(employeeName: "MARIA APARECIDA DOS SANTOS", employeeSigned: "true"));

        Assert.DoesNotContain(findings, f => f.Code == "WORKER_NAME_MISMATCH");
    }

    [Fact]
    public void Should_Warn_When_Worker_Name_Differs()
    {
        var findings = Validate(AnalysisTestData.ReceiptJson(employeeName: "JOANA DA SILVA", employeeSigned: "true"));

        Assert.Contains(findings, f => f.Code == "WORKER_NAME_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Block_Competence_Outside_Of_The_Upload_Period()
    {
        var findings = Validate(AnalysisTestData.ReceiptJson(competence: "06/2026", employeeSigned: "true"));

        var finding = Assert.Single(findings);
        Assert.Equal("COMPETENCE_OUT_OF_PERIOD", finding.Code);
        Assert.Equal(FindingSeverity.Blocking, finding.Severity);
    }

    [Fact]
    public void Should_Skip_Worker_Checks_When_Document_Has_No_Worker_Loaded()
    {
        var findings = Validate(
            AnalysisTestData.ReceiptJson(employeeCpf: "111.444.777-35", employeeSigned: "true"),
            AnalysisTestData.ReceiptDocument(withWorker: false));

        Assert.Empty(findings);
    }
}
