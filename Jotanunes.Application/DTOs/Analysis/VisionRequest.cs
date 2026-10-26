namespace Jotanunes.Application.DTOs.Analysis;

public record VisionRequest(string Instructions, string SchemaName, string JsonSchema, IReadOnlyList<DocumentImage> Images);
