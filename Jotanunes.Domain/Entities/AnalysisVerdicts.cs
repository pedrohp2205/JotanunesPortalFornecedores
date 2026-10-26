using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Entities;

public static class AnalysisVerdicts
{
    public static AnalysisVerdict From(IEnumerable<AnalysisFinding> findings)
    {
        var severities = findings.Select(f => f.Severity).ToList();

        return severities.Contains(FindingSeverity.Blocking) ? AnalysisVerdict.NonConforming
            : severities.Contains(FindingSeverity.Warning) ? AnalysisVerdict.NeedsAttention
            : AnalysisVerdict.Conforming;
    }
}
