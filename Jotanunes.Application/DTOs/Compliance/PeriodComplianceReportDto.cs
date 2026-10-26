using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Compliance;

public class PeriodComplianceReportDto
{
    public long Id { get; set; }
    public long SupplyRequestId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public PeriodComplianceStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public AnalysisVerdict? Verdict { get; set; }
    public string? VerdictDescription { get; set; }
    public List<AnalysisFindingDto> Findings { get; set; } = [];
    public DateTime RequestedAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
}
