using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Projections;

public record ReviewedDocument(
    long DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeName,
    DocumentStatus Status,
    DocumentAnalysisStatus? AnalysisStatus,
    AnalysisVerdict? Verdict);
