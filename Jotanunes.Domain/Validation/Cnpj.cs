using System.Text.RegularExpressions;

namespace Jotanunes.Domain.Validation;

public static class Cnpj
{
    private static readonly int[] FirstDigitWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondDigitWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    // Remove máscara (pontos, barra e traço) deixando apenas os dígitos.
    public static string Normalize(string? cnpj)
    {
        return string.IsNullOrWhiteSpace(cnpj) ? string.Empty : Regex.Replace(cnpj, "[^0-9]", "");
    }

    public static bool IsValid(string? cnpj)
    {
        var digits = Normalize(cnpj);

        if (digits.Length != 14) return false;
        if (digits.Distinct().Count() == 1) return false;

        var baseDigits = digits[..12];
        var firstDigit = CalculateDigit(baseDigits, FirstDigitWeights);
        var secondDigit = CalculateDigit(baseDigits + firstDigit, SecondDigitWeights);

        return digits == baseDigits + firstDigit + secondDigit;
    }

    // Formata o CNPJ no padrão 00.000.000/0000-00.
    public static string Format(string cnpj)
    {
        var digits = Normalize(cnpj);
        if (digits.Length != 14) return cnpj;

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }

    private static int CalculateDigit(string digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
