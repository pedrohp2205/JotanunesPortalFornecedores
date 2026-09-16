using Jotanunes.Application.DTOs.WorkSites;

namespace Jotanunes.Application.Interfaces;

public interface IWorkSiteService
{
    Task<List<WorkSiteDto>> Get();
    Task<WorkSiteDto> GetById(long id);
    Task<List<CompanyWorkSiteDto>> GetByCompany(long companyId);
    Task<WorkSiteDto> Create(WorkSiteCreateDto model);
    Task<WorkSiteDto> Update(long id, WorkSiteUpdateDto model);
    Task<WorkSiteDto> Delete(long id);
    Task<List<WorkSiteCompanyDto>> GetCompanies(long workSiteId);
    Task LinkCompany(long workSiteId, long companyId, int? requiredWorkerCount = null);
    Task UnlinkCompany(long workSiteId, long companyId);
    Task<WorkSiteCompanyDto> UpdateRequiredWorkerCount(long workSiteId, long companyId, int? requiredWorkerCount);
}
