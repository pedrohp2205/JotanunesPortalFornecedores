using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class CrfAnalyzerTests
{
    private static readonly DateOnly Today = new(2026, 7, 20);
    private readonly CrfAnalyzer _analyzer = new();

    private static DocumentText Text(string text) => new([text]);

    private List<AnalysisFinding> Analyze(string text, DateOnly today, DateOnly? informedExpiration = null)
    {
        var extraction = _analyzer.Extract(Text(text));
        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        return _analyzer.Validate(extraction, AnalysisTestData.CrfDocument(informedExpiration), today).ToList();
    }

    [Fact]
    public void Should_Extract_All_Fields()
    {
        var extraction = _analyzer.Extract(Text(AnalysisTestData.CrfText()));

        Assert.True(extraction.IsComplete);
        Assert.Equal("11222333000181", extraction.Get("cnpj"));
        Assert.Equal("11/07/2026", extraction.Get("validFrom"));
        Assert.Equal("09/08/2026", extraction.Get("validUntil"));
        Assert.Equal(AnalysisTestData.CompanyName, extraction.Get("corporateName"));
        Assert.Equal("2026071115160835286076", extraction.Get("certificationNumber"));
    }

    [Fact]
    public void Should_Read_Required_Fields_When_Labels_Come_Before_Values()
    {
        var extraction = _analyzer.Extract(Text(AnalysisTestData.CrfTextLabelsFirst()));

        Assert.True(extraction.IsComplete);
        Assert.Equal("11222333000181", extraction.Get("cnpj"));
        Assert.Equal("09/08/2026", extraction.Get("validUntil"));
    }

    [Fact]
    public void Should_Treat_Cnpj_With_Wrong_Check_Digit_As_Not_Read()
    {
        var extraction = _analyzer.Extract(Text(AnalysisTestData.CrfText(cnpj: "11.222.333/0001-82")));

        Assert.False(extraction.IsComplete);
        Assert.Contains("CNPJ", extraction.Missing);
    }

    [Fact]
    public void Should_Not_Recognize_Other_Documents_As_Crf()
    {
        var extraction = _analyzer.Extract(Text("CERTIDÃO NEGATIVA DE DÉBITOS CNPJ: 11.222.333/0001-81 Válida até 11/01/2027."));

        Assert.False(extraction.IsComplete);
        Assert.Contains("identificação como CRF do FGTS", extraction.Missing);
    }

    [Fact]
    public void Should_Report_Nothing_Wrong_For_Valid_Crf()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), Today, informedExpiration: new DateOnly(2026, 8, 9));

        Assert.Empty(findings);
    }

    [Fact]
    public void Should_Block_Crf_From_Another_Company()
    {
        var findings = Analyze(AnalysisTestData.CrfText(cnpj: "02.811.737/0001-10"), Today);

        var finding = Assert.Single(findings, f => f.Code == "CNPJ_MISMATCH");
        Assert.Equal(FindingSeverity.Blocking, finding.Severity);
    }

    [Fact]
    public void Should_Block_Expired_Crf()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), new DateOnly(2026, 9, 28));

        var finding = Assert.Single(findings, f => f.Code == "EXPIRED");
        Assert.Equal(FindingSeverity.Blocking, finding.Severity);
        Assert.Contains("09/08/2026", finding.Message);
    }

    [Fact]
    public void Should_Accept_Crf_On_Its_Last_Valid_Day()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), new DateOnly(2026, 8, 9));

        Assert.DoesNotContain(findings, f => f.Code == "EXPIRED");
        Assert.Contains(findings, f => f.Code == "EXPIRING_SOON");
    }

    [Fact]
    public void Should_Warn_When_Crf_Expires_Soon()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), new DateOnly(2026, 8, 3));

        var finding = Assert.Single(findings, f => f.Code == "EXPIRING_SOON");
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void Should_Warn_When_Crf_Is_Not_Valid_Yet()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), new DateOnly(2026, 7, 1));

        Assert.Contains(findings, f => f.Code == "NOT_YET_VALID" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Warn_When_Informed_Expiration_Differs_From_Certificate()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), Today, informedExpiration: new DateOnly(2026, 12, 31));

        Assert.Contains(findings, f => f.Code == "EXPIRATION_DATE_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Suggest_Expiration_Date_When_Not_Informed()
    {
        var findings = Analyze(AnalysisTestData.CrfText(), Today);

        var finding = Assert.Single(findings, f => f.Code == "EXPIRATION_DATE_DETECTED");
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    [Fact]
    public void Should_Ignore_Accents_And_Punctuation_When_Comparing_Corporate_Name()
    {
        var findings = Analyze(AnalysisTestData.CrfText(corporateName: "CONSTRUTORA EXEMPLO LTDA."), Today, new DateOnly(2026, 8, 9));

        Assert.DoesNotContain(findings, f => f.Code == "CORPORATE_NAME_MISMATCH");
    }

    [Fact]
    public void Should_Warn_When_Corporate_Name_Differs()
    {
        var findings = Analyze(AnalysisTestData.CrfText(corporateName: "OUTRA EMPRESA LTDA"), Today, new DateOnly(2026, 8, 9));

        Assert.Contains(findings, f => f.Code == "CORPORATE_NAME_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Warn_When_Validity_Is_Inverted()
    {
        var findings = Analyze(AnalysisTestData.CrfText(validity: "09/08/2026 a 11/07/2026"), Today);

        Assert.Contains(findings, f => f.Code == "INVALID_VALIDITY");
    }
}
