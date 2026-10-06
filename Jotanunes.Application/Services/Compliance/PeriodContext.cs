using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Compliance;

public record PeriodDocument(Document Document, FieldExtraction? Fields, TextExtractionEngine? Engine)
{
    public string TypeCode => Document.DocumentType.Code;
    public bool IsRead => Fields is not null;
    public bool ReadByVision => Engine == TextExtractionEngine.Vision;
}

public record PeriodContext(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<PeriodDocument> Documents,
    IReadOnlyList<Worker> AllocatedWorkers);
