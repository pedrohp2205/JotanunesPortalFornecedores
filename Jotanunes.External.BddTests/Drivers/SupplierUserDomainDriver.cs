using System.Security.Cryptography;
using System.Text;

using Jotanunes.Domain.Entities;
using Jotanunes.Infra.Security.Services;

namespace Jotanunes.External.BddTests.Drivers;

public class SupplierUserDomainDriver
{
    private static readonly BCryptPasswordHasher PasswordHasher = new();

    public static SupplierUser CriarUsuarioValido(
        long companyId,
        string email,
        string senha,
        string? nome = null,
        bool deveTrocarSenha = false,
        bool ativo = true)
    {
        var usuario = new SupplierUser(
            companyId,
            nome ?? "Fornecedor Teste",
            email,
            PasswordHasher.Hash(senha),
            deveTrocarSenha);

        if (!ativo)
        {
            usuario.Deactivate();
        }

        return usuario;
    }

    public static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
