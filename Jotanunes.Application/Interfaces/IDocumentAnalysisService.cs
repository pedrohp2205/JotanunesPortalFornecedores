using Jotanunes.Application.DTOs.Analysis;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentAnalysisService
{
    Task<List<long>> GetPendingIds(int take);
    Task Analyze(long analysisId, CancellationToken cancellationToken = default);
    Task<DocumentAnalysisDto> GetByDocument(long documentId);
    Task<DocumentAnalysisDto> Reanalyze(long documentId);
    Task<AnalysisMetricsDto> GetMetrics(DateOnly? from, DateOnly? to);
}
