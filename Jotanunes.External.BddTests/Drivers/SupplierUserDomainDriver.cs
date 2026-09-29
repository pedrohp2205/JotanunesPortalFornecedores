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
        bool deveTrocarSenha = false)
    {
        return new SupplierUser(
            companyId,
            nome ?? "Fornecedor Teste",
            email,
            PasswordHasher.Hash(senha),
            deveTrocarSenha);
    }
}
