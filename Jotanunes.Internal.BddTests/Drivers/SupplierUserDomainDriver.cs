using Jotanunes.Application.DTOs.Users;
using Jotanunes.Domain.Entities;
using Jotanunes.Infra.Security.Services;

namespace Jotanunes.Internal.BddTests.Drivers;

public class SupplierUserDomainDriver
{
    private static readonly BCryptPasswordHasher PasswordHasher = new();

    public static SupplierUser CriarUsuarioValido(long companyId, string email, string senha, bool ativo = true)
    {
        var usuario = new SupplierUser(companyId, "Fornecedor Teste", email, PasswordHasher.Hash(senha), mustChangePassword: false);
        if (!ativo)
        {
            usuario.Deactivate();
        }
        return usuario;
    }

    public static SupplierUserCreateDto CriarSupplierUserCreateDtoValido(string email, string senhaProvisoria)
    {
        return new SupplierUserCreateDto
        {
            Name = "Maria Souza",
            Email = email,
            TemporaryPassword = senhaProvisoria
        };
    }
}
