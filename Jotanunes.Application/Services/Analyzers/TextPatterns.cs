using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public static partial class TextPatterns
{
    public static string? FirstValidCnpj(string text)
    {
        var match = CnpjPattern().Match(text);
        return match.Success && Cnpj.IsValid(match.Value) ? Cnpj.Normalize(match.Value) : null;
    }

    public static string? ValidCnpj(string? value)
    {
        return Cnpj.IsValid(value) ? Cnpj.Normalize(value) : null;
    }

    public static string? ValidCpf(string? value)
    {
        return Cpf.IsValid(value) ? Cpf.Normalize(value) : null;
    }

    public static DateOnly? ParseCompetence(string? value)
    {
        return DateOnly.TryParseExact(value, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
            ? month
            : null;
    }

    public static string FormatMoney(decimal value)
    {
        return value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    public static decimal? ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("R$", string.Empty).Replace(" ", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var amount) ? amount : null;
    }

    public static string FormatAmount(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static decimal? ParseAmount(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : null;
    }

    public static string? NormalizeMaskedCpf(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var characters = value.Where(c => char.IsDigit(c) || c == '*').ToArray();
        return characters.Length == 11 && characters.Any(char.IsDigit) ? new string(characters) : null;
    }

    public static bool MaskedCpfMatches(string maskedCpf, string cpf)
    {
        return maskedCpf.Length == cpf.Length && maskedCpf.Zip(cpf).All(pair => pair.First == '*' || pair.First == pair.Second);
    }

    public static int? ParseHours(string? value)
    {
        var match = value is null ? Match.Empty : HoursPattern().Match(value);
        return match.Success ? int.Parse(match.Groups["h"].Value) * 60 + int.Parse(match.Groups["m"].Value) : null;
    }

    public static string FormatHours(int minutes)
    {
        return $"{minutes / 60}:{minutes % 60:D2}";
    }

    public static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    public static string FormatCompetence(DateOnly month)
    {
        return month.ToString("MM/yyyy", CultureInfo.InvariantCulture);
    }

    public static string FormatDate(DateOnly date)
    {
        return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    public static string NormalizeName(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : ' ');
        }

        return Whitespace().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"^\s*(?<h>\d{1,3}):(?<m>[0-5]\d)\s*$")]
    private static partial Regex HoursPattern();

    [GeneratedRegex(@"\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}")]
    private static partial Regex CnpjPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
