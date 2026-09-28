using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Domain.Interfaces;

public interface ISupplyRequestRepository : IGenericRepository<SupplyRequest>
{
    Task<PageList<SupplyRequest>> Get(PageParams pageParams, SupplyRequestFilter filter);
    Task<List<SupplyRequest>> GetAll(SupplyRequestFilter filter, bool activeOnly = false);
    Task<SupplyRequest?> GetById(long id);
    Task<bool> HasActive(long companyId, Enums.SupplierType supplierTypes);
    Task<bool> ActiveExists(long companyId, long workSiteId, Enums.SupplierType supplierType);
}
