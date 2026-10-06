using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class InternalDocumentFilter : DocumentFilter
{
    public DocumentAnalysisStatus? AnalysisStatus { get; set; }
    public AnalysisVerdict? AnalysisVerdict { get; set; }
}
