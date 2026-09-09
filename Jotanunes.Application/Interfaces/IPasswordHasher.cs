namespace Jotanunes.Application.Interfaces;

// Porta para o algoritmo de hash de senha. A implementação fica em Infra.Security.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
