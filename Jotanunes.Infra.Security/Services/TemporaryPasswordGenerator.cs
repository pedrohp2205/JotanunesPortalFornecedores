using System.Security.Cryptography;
using Jotanunes.Application.Interfaces;

namespace Jotanunes.Infra.Security.Services;

// Senha provisória enviada por e-mail. O alfabeto não tem caracteres ambíguos
// (O/0, l/I/1) porque o fornecedor costuma digitar a senha lendo do e-mail.
public class TemporaryPasswordGenerator : IPasswordGenerator
{
    private const int Length = 12;
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*";

    public string Generate()
    {
        const string alphabet = Uppercase + Lowercase + Digits + Symbols;

        var password = new char[Length];
        password[0] = Pick(Uppercase);
        password[1] = Pick(Lowercase);
        password[2] = Pick(Digits);
        password[3] = Pick(Symbols);

        for (var i = 4; i < Length; i++)
        {
            password[i] = Pick(alphabet);
        }

        RandomNumberGenerator.Shuffle(password.AsSpan());

        return new string(password);
    }

    private static char Pick(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }
}
