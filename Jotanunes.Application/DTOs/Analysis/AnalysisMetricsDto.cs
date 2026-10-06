namespace Jotanunes.Application.DTOs.Analysis;

public class AnalysisMetricsDto
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public AnalysisTypeMetricsDto Total { get; set; } = new();
    public List<AnalysisTypeMetricsDto> DocumentTypes { get; set; } = [];
}

public class AnalysisTypeMetricsDto
{
    public string? DocumentTypeCode { get; set; }
    public string? DocumentTypeName { get; set; }
    public int Reviewed { get; set; }
    public int NotAnalyzed { get; set; }
    public int Agreements { get; set; }
    public int FalseAlarms { get; set; }
    public int MissedProblems { get; set; }
    public int AttentionApproved { get; set; }
    public int AttentionRejected { get; set; }
    public decimal? AgreementRate { get; set; }
}
