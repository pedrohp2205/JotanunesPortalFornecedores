using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;

namespace Jotanunes.Application.Interfaces;

public interface IVisionAnalyzer : IDocumentTypeAnalyzer
{
    string Instructions { get; }
    string SchemaName { get; }
    string JsonSchema { get; }
    FieldExtraction FromVision(JsonElement result);
}
