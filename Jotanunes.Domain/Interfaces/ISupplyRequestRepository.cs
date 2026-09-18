using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;

namespace Jotanunes.Domain.Interfaces;

public interface ISupplyRequestRepository : IGenericRepository<SupplyRequest>
{
    Task<List<SupplyRequest>> Get(SupplyRequestFilter filter);
    Task<SupplyRequest?> GetById(long id);
    Task<List<SupplyRequest>> GetActive();
    Task<bool> HasActive(long companyId, Enums.SupplierType supplierTypes);
    Task<bool> ActiveExists(long companyId, long workSiteId, Enums.SupplierType supplierType);
}
