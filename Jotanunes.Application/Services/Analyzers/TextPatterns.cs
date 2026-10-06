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

    public static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
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

    [GeneratedRegex(@"\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}")]
    private static partial Regex CnpjPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
