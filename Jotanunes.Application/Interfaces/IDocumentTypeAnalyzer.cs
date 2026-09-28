using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentTypeAnalyzer
{
    IReadOnlyCollection<string> DocumentTypeCodes { get; }
    FieldExtraction Extract(DocumentText text);
    IReadOnlyList<AnalysisFinding> Validate(FieldExtraction extraction, Document document, DateOnly today);
}
