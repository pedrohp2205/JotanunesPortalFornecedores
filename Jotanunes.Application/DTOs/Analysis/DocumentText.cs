using System.Text.RegularExpressions;

namespace Jotanunes.Application.DTOs.Analysis;

public partial class DocumentText
{
    public static readonly DocumentText Empty = new([]);

    public IReadOnlyList<string> Pages { get; }

    public string Flat { get; }

    public DocumentText(IReadOnlyList<string> pages)
    {
        Pages = pages;
        Flat = Whitespace().Replace(string.Join(' ', pages), " ").Trim();
    }

    public bool IsEmpty => Flat.Length == 0;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
