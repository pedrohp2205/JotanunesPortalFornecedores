using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class PeriodComplianceReport : BaseEntity
{
    public long SupplyRequestId { get; private set; }
    public SupplyRequest SupplyRequest { get; private set; } = null!;

    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    public PeriodComplianceStatus Status { get; private set; }
    public AnalysisVerdict? Verdict { get; private set; }
    public List<AnalysisFinding> Findings { get; private set; } = [];
    public DateTime RequestedAt { get; private set; }
    public DateTime? GeneratedAt { get; private set; }

    protected PeriodComplianceReport() { }

    public PeriodComplianceReport(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd, DateTime now)
    {
        JotanunesException.When(periodStart > periodEnd, "Período inválido.");

        SupplyRequestId = supplyRequestId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Status = PeriodComplianceStatus.Pending;
        RequestedAt = now;
    }

    public void RequestRecalculation(DateTime now)
    {
        Status = PeriodComplianceStatus.Pending;
        RequestedAt = now;
    }

    public void Complete(IEnumerable<AnalysisFinding> findings, DateTime startedAt, DateTime now)
    {
        Findings = findings.ToList();
        Verdict = AnalysisVerdicts.From(Findings);
        GeneratedAt = now;
        Status = RequestedAt > startedAt ? PeriodComplianceStatus.Pending : PeriodComplianceStatus.Completed;
    }
}
