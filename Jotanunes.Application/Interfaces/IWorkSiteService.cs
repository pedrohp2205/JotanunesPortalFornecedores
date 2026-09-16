using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.WorkSites;

namespace Jotanunes.Application.Interfaces;

public interface IWorkSiteService
{
    Task<List<WorkSiteDto>> Get();
    Task<WorkSiteDto> GetById(long id);
    Task<List<WorkSiteDto>> GetByCompany(long companyId);
    Task<WorkSiteDto> Create(WorkSiteCreateDto model);
    Task<WorkSiteDto> Update(long id, WorkSiteUpdateDto model);
    Task<WorkSiteDto> Delete(long id);
    Task<List<CompanyDto>> GetCompanies(long workSiteId);
    Task LinkCompany(long workSiteId, long companyId);
    Task UnlinkCompany(long workSiteId, long companyId);
}
