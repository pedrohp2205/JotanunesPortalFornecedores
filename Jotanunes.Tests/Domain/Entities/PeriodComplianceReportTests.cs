using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests.Domain.Entities;

public class PeriodComplianceReportTests
{
    private static readonly DateTime Requested = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private static PeriodComplianceReport NewReport() => new(3, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), Requested);

    [Fact]
    public void Should_Start_Pending()
    {
        var report = NewReport();

        Assert.Equal(PeriodComplianceStatus.Pending, report.Status);
        Assert.Null(report.Verdict);
    }

    [Fact]
    public void Should_Reject_Inverted_Period()
    {
        Assert.Throws<JotanunesException>(() => new PeriodComplianceReport(3, new DateOnly(2026, 7, 31), new DateOnly(2026, 7, 1), Requested));
    }

    [Fact]
    public void Should_Complete_With_Verdict_From_Findings()
    {
        var report = NewReport();

        report.Complete([new AnalysisFinding("PAYMENT_AMOUNT_MISMATCH", FindingSeverity.Blocking, "diferente")], Requested.AddSeconds(1), Requested.AddSeconds(2));

        Assert.Equal(PeriodComplianceStatus.Completed, report.Status);
        Assert.Equal(AnalysisVerdict.NonConforming, report.Verdict);
        Assert.Equal(Requested.AddSeconds(2), report.GeneratedAt);
    }

    [Fact]
    public void Should_Stay_Pending_When_Recalculation_Was_Requested_During_The_Calculation()
    {
        var report = NewReport();
        var startedAt = Requested.AddSeconds(1);
        report.RequestRecalculation(Requested.AddSeconds(5));

        report.Complete([], startedAt, Requested.AddSeconds(6));

        Assert.Equal(PeriodComplianceStatus.Pending, report.Status);
        Assert.Equal(AnalysisVerdict.Conforming, report.Verdict);
    }
}
