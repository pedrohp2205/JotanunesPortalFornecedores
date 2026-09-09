using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface ISupplierUserRepository : IGenericRepository<SupplierUser>
{
    Task<SupplierUser?> GetById(long id);
    Task<SupplierUser?> GetByEmail(string email);
    Task<SupplierUser?> GetByRefreshToken(string refreshToken);
    Task<List<SupplierUser>> GetByCompany(long companyId);
    Task<bool> EmailInUse(string email, long? ignoreUserId = null);
}
