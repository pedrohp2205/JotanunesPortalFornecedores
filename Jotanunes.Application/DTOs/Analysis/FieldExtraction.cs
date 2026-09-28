using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.DTOs.Analysis;

public class FieldExtraction
{
    public List<ExtractedField> Fields { get; } = [];

    public List<string> Missing { get; } = [];

    public bool IsComplete => Missing.Count == 0;

    public string? Get(string name)
    {
        return Fields.FirstOrDefault(f => f.Name == name)?.Value;
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
