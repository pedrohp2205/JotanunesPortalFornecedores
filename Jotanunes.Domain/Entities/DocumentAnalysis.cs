using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class DocumentAnalysis : BaseEntity
{
    public const string WrongDocumentTypeCode = "WRONG_DOCUMENT_TYPE";
    public const int MaxAttempts = 3;
    public const int MaxFailureReasonLength = 500;
    public static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(1);

    public long DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;

    public DocumentAnalysisStatus Status { get; private set; }
    public AnalysisVerdict? Verdict { get; private set; }
    public TextExtractionEngine? Engine { get; private set; }
    public List<ExtractedField> Fields { get; private set; } = [];
    public List<AnalysisFinding> Findings { get; private set; } = [];
    public string? FailureReason { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? AnalyzedAt { get; private set; }

    public bool FoundWrongDocument => Findings.Any(f => f.Code == WrongDocumentTypeCode);

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

        Findings = engine == TextExtractionEngine.Vision
            ? findings.Select(CapVisionSeverity).ToList()
            : findings.ToList();
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

    public void RegisterFailure(string reason, bool transient, DateTime now)
    {
        EnsurePending();

        Attempts++;
        FailureReason = Truncate(reason);

        if (!transient && Attempts >= MaxAttempts)
        {
            Status = DocumentAnalysisStatus.Failed;
            NextAttemptAt = null;
            AnalyzedAt = now;
            return;
        }

        NextAttemptAt = now + RetryDelay(Attempts);
    }

    public static TimeSpan RetryDelay(int attempts)
    {
        var delay = FirstRetryDelay * Math.Pow(2, Math.Min(attempts - 1, 16));
        return delay < MaxRetryDelay ? delay : MaxRetryDelay;
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
        NextAttemptAt = null;
        AnalyzedAt = null;
    }

    private static AnalysisFinding CapVisionSeverity(AnalysisFinding finding)
    {
        return finding.Severity == FindingSeverity.Blocking
            ? new AnalysisFinding(finding.Code, FindingSeverity.Warning, finding.Message)
            : finding;
    }

    private void Finish(DocumentAnalysisStatus status, string? reason)
    {
        Status = status;
        NextAttemptAt = null;
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
