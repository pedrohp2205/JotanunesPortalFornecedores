namespace Jotanunes.Domain.Entities;

public class ExtractedField
{
    public string Name { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    protected ExtractedField() { }

    public ExtractedField(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
