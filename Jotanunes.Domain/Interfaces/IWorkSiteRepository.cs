using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkSiteRepository : IGenericRepository<WorkSite>
{
    Task<List<WorkSite>> Get();
    Task<WorkSite?> GetById(long id);
    Task<List<WorkSite>> GetByCompany(long companyId);
    Task<List<Company>> GetCompanies(long workSiteId);
    Task<CompanyWorkSite?> GetLink(long workSiteId, long companyId);
    void AddLink(CompanyWorkSite link);
    void RemoveLink(CompanyWorkSite link);
}
