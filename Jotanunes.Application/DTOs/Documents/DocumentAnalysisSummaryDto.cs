using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Documents;

public class DocumentAnalysisSummaryDto
{
    public DocumentAnalysisStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public AnalysisVerdict? Verdict { get; set; }
    public string? VerdictDescription { get; set; }
    public int BlockingCount { get; set; }
    public int WarningCount { get; set; }
    public DateTime? AnalyzedAt { get; set; }
}
