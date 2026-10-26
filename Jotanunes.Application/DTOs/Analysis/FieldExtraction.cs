using System.Text.Json;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.DTOs.Analysis;

public class FieldExtraction
{
    public List<ExtractedField> Fields { get; } = [];

    public List<string> Missing { get; } = [];

    public static FieldExtraction FromFields(IEnumerable<ExtractedField> fields)
    {
        var extraction = new FieldExtraction();
        extraction.Fields.AddRange(fields);
        return extraction;
    }

    public string? WrongDocument { get; private set; }

    public bool IsComplete => Missing.Count == 0 && WrongDocument is null;

    public void MarkWrongDocument(string? detectedDocument)
    {
        WrongDocument = string.IsNullOrWhiteSpace(detectedDocument) ? "outro documento" : detectedDocument.Trim();
    }

    public string? Get(string name)
    {
        return Fields.FirstOrDefault(f => f.Name == name)?.Value;
    }

    private static readonly JsonSerializerOptions ListJson = new(JsonSerializerDefaults.Web);

    public void AddList<T>(string name, IReadOnlyCollection<T> items, string? requiredLabel = null)
    {
        Add(name, items.Count == 0 ? null : JsonSerializer.Serialize(items, ListJson), requiredLabel);
    }

    public List<T> GetList<T>(string name)
    {
        var value = Get(name);
        return value is null ? [] : JsonSerializer.Deserialize<List<T>>(value, ListJson) ?? [];
    }

    public void Add(string name, string? value, string? requiredLabel = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Fields.Add(new ExtractedField(name, value.Trim()));
        }
        else if (requiredLabel is not null)
        {
            Missing.Add(requiredLabel);
        }
    }
}
