using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Interfaces;

// Porta para emissão de tokens. A implementação JWT fica em Infra.Security.
public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(SupplierUser user);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
}
