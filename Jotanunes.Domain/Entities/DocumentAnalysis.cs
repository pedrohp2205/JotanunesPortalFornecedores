using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class DocumentAnalysis : BaseEntity
{
    public const int MaxAttempts = 3;
    public const int MaxFailureReasonLength = 500;

    public long DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;

    public DocumentAnalysisStatus Status { get; private set; }
    public AnalysisVerdict? Verdict { get; private set; }
    public TextExtractionEngine? Engine { get; private set; }
    public List<ExtractedField> Fields { get; private set; } = [];
    public List<AnalysisFinding> Findings { get; private set; } = [];
    public string? FailureReason { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? AnalyzedAt { get; private set; }

    protected DocumentAnalysis() { }

    public DocumentAnalysis(Document document)
    {
        Document = document;
        DocumentId = document.Id;
        Status = DocumentAnalysisStatus.Pending;
    }

    public void Complete(TextExtractionEngine engine, IEnumerable<ExtractedField> fields, IEnumerable<AnalysisFinding> findings)
    {
        EnsurePending();

        Findings = findings.ToList();
        Fields = fields.ToList();
        Engine = engine;
        Verdict = Findings.Any(f => f.Severity == FindingSeverity.Blocking) ? AnalysisVerdict.NonConforming
            : Findings.Any(f => f.Severity == FindingSeverity.Warning) ? AnalysisVerdict.NeedsAttention
            : AnalysisVerdict.Conforming;

        Finish(DocumentAnalysisStatus.Completed, null);
    }

    public void RequireManualReview(TextExtractionEngine? engine, IEnumerable<ExtractedField> fields, string reason)
    {
        EnsurePending();

        Fields = fields.ToList();
        Findings = [];
        Engine = engine;
        Verdict = null;

        Finish(DocumentAnalysisStatus.ManualReviewRequired, reason);
    }

    public void MarkNotSupported()
    {
        EnsurePending();

        Finish(DocumentAnalysisStatus.NotSupported, null);
    }

    public void RegisterFailure(string reason)
    {
        EnsurePending();

        Attempts++;
        FailureReason = Truncate(reason);

        if (Attempts >= MaxAttempts)
        {
            Status = DocumentAnalysisStatus.Failed;
            AnalyzedAt = DateTime.UtcNow;
        }
    }

    public void Restart()
    {
        JotanunesException.When(Status == DocumentAnalysisStatus.Pending, "A análise deste documento já está na fila.");

        Status = DocumentAnalysisStatus.Pending;
        Verdict = null;
        Engine = null;
        Fields = [];
        Findings = [];
        FailureReason = null;
        Attempts = 0;
        AnalyzedAt = null;
    }

    private void Finish(DocumentAnalysisStatus status, string? reason)
    {
        Status = status;
        FailureReason = reason is null ? null : Truncate(reason);
        AnalyzedAt = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        JotanunesException.When(Status != DocumentAnalysisStatus.Pending, "A análise deste documento já foi concluída.");
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxFailureReasonLength ? value : value[..MaxFailureReasonLength];
    }
}
