using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkSiteRepository : IGenericRepository<WorkSite>
{
    Task<List<WorkSite>> Get();
    Task<WorkSite?> GetById(long id);
    Task<List<CompanyWorkSite>> GetByCompany(long companyId);
    Task<List<CompanyWorkSite>> GetCompanies(long workSiteId);
    Task<CompanyWorkSite?> GetLink(long workSiteId, long companyId);
    Task<CompanyWorkSite?> GetLinkById(long id);
    void AddLink(CompanyWorkSite link);
    void RemoveLink(CompanyWorkSite link);
}
