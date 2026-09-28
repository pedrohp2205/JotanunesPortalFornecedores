using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface ISupplierUserSessionRepository : IGenericRepository<SupplierUserSession>
{
    Task<SupplierUserSession?> GetById(long id);
    Task<SupplierUserSession?> GetByRefreshTokenHash(string refreshTokenHash);
}
