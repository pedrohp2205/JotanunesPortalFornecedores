using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Analysis;

public class DocumentAnalysisDto
{
    public long Id { get; set; }
    public long DocumentId { get; set; }

    public DocumentAnalysisStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public AnalysisVerdict? Verdict { get; set; }
    public string? VerdictDescription { get; set; }
    public TextExtractionEngine? Engine { get; set; }
    public string? EngineDescription { get; set; }

    public List<ExtractedFieldDto> Fields { get; set; } = [];
    public List<AnalysisFindingDto> Findings { get; set; } = [];

    public string? FailureReason { get; set; }
    public int Attempts { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExtractedFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AnalysisFindingDto
{
    public string Code { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public string SeverityDescription { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
