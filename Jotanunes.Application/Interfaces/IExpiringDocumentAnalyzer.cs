using Jotanunes.Application.DTOs.Analysis;

namespace Jotanunes.Application.Interfaces;

public interface IExpiringDocumentAnalyzer : IDocumentTypeAnalyzer
{
    DateOnly? ReadExpirationDate(FieldExtraction extraction);
}
