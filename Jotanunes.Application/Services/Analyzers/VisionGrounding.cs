using System.Text.RegularExpressions;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public static partial class VisionGrounding
{
    public const string NotConfirmedCode = "VISION_NOT_CONFIRMED_BY_OCR";
    public const int MinimumOcrDigits = 20;
    public const int MinimumValueDigits = 6;

    public static AnalysisFinding? Check(FieldExtraction extraction, DocumentText? ocrText)
    {
        if (ocrText is null)
        {
            return null;
        }

        var ocrDigits = Digits(MonthNames().Replace(ocrText.Flat, match => $"{MonthNumber(match.Groups["month"].Value):D2}/{match.Groups["year"].Value}"));
        if (ocrDigits.Length < MinimumOcrDigits)
        {
            return null;
        }

        var unconfirmed = extraction.Fields
            .Where(field => field.Name != "documentKind" && !field.Value.StartsWith('[') && !field.Value.Contains('*'))
            .Select(field => field.Value)
            .Where(value => Digits(value) is { Length: >= MinimumValueDigits } digits && !ocrDigits.Contains(digits, StringComparison.Ordinal))
            .Distinct()
            .ToList();

        return unconfirmed.Count == 0
            ? null
            : new AnalysisFinding(
                NotConfirmedCode,
                FindingSeverity.Warning,
                $"Valores lidos pela IA que não aparecem no texto da página (OCR): {string.Join(", ", unconfirmed)}. Confira no documento.");
    }

    private static readonly string[] Months =
    [
        "JANEIRO", "FEVEREIRO", "MARCO", "ABRIL", "MAIO", "JUNHO", "JULHO", "AGOSTO", "SETEMBRO", "OUTUBRO", "NOVEMBRO", "DEZEMBRO"
    ];

    private static int MonthNumber(string name)
    {
        return Array.IndexOf(Months, TextPatterns.NormalizeName(name)) + 1;
    }

    [GeneratedRegex(@"(?<month>janeiro|fevereiro|mar[cç]o|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro)\s*(?:de|/)?\s*(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex MonthNames();

    private static string Digits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }
}
