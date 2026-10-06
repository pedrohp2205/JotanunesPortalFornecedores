using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Domain.Entities;

public class DocumentAnalysisTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    private static DocumentAnalysis NewAnalysis() => new(AnalysisTestData.CrfDocument());

    [Fact]
    public void Should_Be_NonConforming_When_Any_Finding_Is_Blocking()
    {
        var analysis = NewAnalysis();

        analysis.Complete(TextExtractionEngine.NativeText, [], [
            new AnalysisFinding("EXPIRING_SOON", FindingSeverity.Warning, "vence logo"),
            new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "vencida")
        ]);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        Assert.Equal(AnalysisVerdict.NonConforming, analysis.Verdict);
    }

    [Fact]
    public void Should_Need_Attention_When_Worst_Finding_Is_Warning()
    {
        var analysis = NewAnalysis();

        analysis.Complete(TextExtractionEngine.NativeText, [], [new AnalysisFinding("EXPIRING_SOON", FindingSeverity.Warning, "vence logo")]);

        Assert.Equal(AnalysisVerdict.NeedsAttention, analysis.Verdict);
    }

    [Fact]
    public void Should_Be_Conforming_When_There_Are_Only_Info_Findings()
    {
        var analysis = NewAnalysis();

        analysis.Complete(TextExtractionEngine.NativeText, [], [new AnalysisFinding("EXPIRATION_DATE_DETECTED", FindingSeverity.Info, "validade")]);

        Assert.Equal(AnalysisVerdict.Conforming, analysis.Verdict);
    }

    [Fact]
    public void Should_Stay_Pending_With_Next_Attempt_Until_Max_Attempts_Then_Fail()
    {
        var analysis = NewAnalysis();

        for (var attempt = 1; attempt < DocumentAnalysis.MaxAttempts; attempt++)
        {
            analysis.RegisterFailure("bucket fora do ar", transient: false, Now);
            Assert.Equal(DocumentAnalysisStatus.Pending, analysis.Status);
            Assert.Equal(Now + DocumentAnalysis.RetryDelay(attempt), analysis.NextAttemptAt);
        }

        analysis.RegisterFailure("bucket fora do ar", transient: false, Now);

        Assert.Equal(DocumentAnalysisStatus.Failed, analysis.Status);
        Assert.Equal(DocumentAnalysis.MaxAttempts, analysis.Attempts);
        Assert.Null(analysis.NextAttemptAt);
    }

    [Fact]
    public void Should_Never_Fail_On_Transient_Errors()
    {
        var analysis = NewAnalysis();

        for (var attempt = 1; attempt <= 20; attempt++)
        {
            analysis.RegisterFailure("OpenRouter retornou 429", transient: true, Now);
        }

        Assert.Equal(DocumentAnalysisStatus.Pending, analysis.Status);
        Assert.Equal(Now + DocumentAnalysis.MaxRetryDelay, analysis.NextAttemptAt);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(7, 1920)]
    [InlineData(8, 3600)]
    [InlineData(50, 3600)]
    public void Should_Double_Retry_Delay_Up_To_One_Hour(int attempts, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), DocumentAnalysis.RetryDelay(attempts));
    }

    [Fact]
    public void Should_Clear_Next_Attempt_When_Completed()
    {
        var analysis = NewAnalysis();
        analysis.RegisterFailure("OpenRouter retornou 503", transient: true, Now);

        analysis.Complete(TextExtractionEngine.Vision, [], []);

        Assert.Null(analysis.NextAttemptAt);
    }

    [Fact]
    public void Should_Cap_Findings_Read_By_Vision_At_Warning()
    {
        var analysis = NewAnalysis();

        analysis.Complete(TextExtractionEngine.Vision, [], [new AnalysisFinding("WORKER_CPF_MISMATCH", FindingSeverity.Blocking, "CPF diferente")]);

        Assert.Equal(FindingSeverity.Warning, Assert.Single(analysis.Findings).Severity);
        Assert.Equal(AnalysisVerdict.NeedsAttention, analysis.Verdict);
    }

    [Fact]
    public void Should_Keep_Blocking_Findings_Read_From_Text()
    {
        var analysis = NewAnalysis();

        analysis.Complete(TextExtractionEngine.NativeText, [], [new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "vencida")]);

        Assert.Equal(FindingSeverity.Blocking, Assert.Single(analysis.Findings).Severity);
        Assert.Equal(AnalysisVerdict.NonConforming, analysis.Verdict);
    }

    [Fact]
    public void Should_Truncate_Long_Failure_Reason()
    {
        var analysis = NewAnalysis();

        analysis.RegisterFailure(new string('x', 2000), transient: false, Now);

        Assert.Equal(DocumentAnalysis.MaxFailureReasonLength, analysis.FailureReason!.Length);
    }

    [Fact]

    public void Should_Not_Complete_Twice()
    {
        var analysis = NewAnalysis();
        analysis.Complete(TextExtractionEngine.NativeText, [], []);

        Assert.Throws<JotanunesException>(() => analysis.Complete(TextExtractionEngine.NativeText, [], []));
    }

    [Fact]
    public void Should_Clear_Previous_Result_On_Restart()
    {
        var analysis = NewAnalysis();
        analysis.Complete(TextExtractionEngine.NativeText, [new ExtractedField("cnpj", "11222333000181")], [
            new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "vencida")
        ]);

        analysis.Restart();

        Assert.Equal(DocumentAnalysisStatus.Pending, analysis.Status);
        Assert.Null(analysis.Verdict);
        Assert.Empty(analysis.Fields);
        Assert.Empty(analysis.Findings);
    }

    [Fact]
    public void Should_Not_Restart_While_Pending()
    {
        Assert.Throws<JotanunesException>(() => NewAnalysis().Restart());
    }
}
