using System.Text.RegularExpressions;

namespace Jotanunes.Domain.Validation;

public static class Cpf
{
    private static readonly int[] FirstDigitWeights = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondDigitWeights = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    public static string Normalize(string? cpf)
    {
        return string.IsNullOrWhiteSpace(cpf) ? string.Empty : Regex.Replace(cpf, "[^0-9]", "");
    }

    public static bool IsValid(string? cpf)
    {
        var digits = Normalize(cpf);

        if (digits.Length != 11) return false;
        if (digits.Distinct().Count() == 1) return false;

        var baseDigits = digits[..9];
        var firstDigit = CalculateDigit(baseDigits, FirstDigitWeights);
        var secondDigit = CalculateDigit(baseDigits + firstDigit, SecondDigitWeights);

        return digits == baseDigits + firstDigit + secondDigit;
    }

    public static string Format(string cpf)
    {
        var digits = Normalize(cpf);
        if (digits.Length != 11) return cpf;

        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
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
