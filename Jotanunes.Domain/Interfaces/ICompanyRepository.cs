using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Domain.Interfaces;

public interface ICompanyRepository : IGenericRepository<Company>
{
    Task<PageList<Company>> Get(PageParams pageParams, CompanyFilter filter);
    Task<Company?> GetById(long id);
    Task<Company?> GetByCnpj(string cnpj);
    Task<bool> CnpjInUse(string cnpj, long? ignoreCompanyId = null);
}
