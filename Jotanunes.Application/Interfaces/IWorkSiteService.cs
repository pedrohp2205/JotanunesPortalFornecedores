using Jotanunes.Application.DTOs.WorkSites;

namespace Jotanunes.Application.Interfaces;

public interface IWorkSiteService
{
    Task<List<WorkSiteDto>> Get();
    Task<WorkSiteDto> GetById(long id);
    Task<WorkSiteDto> Create(WorkSiteCreateDto model);
    Task<WorkSiteDto> Update(long id, WorkSiteUpdateDto model);
    Task<WorkSiteDto> Delete(long id);
}
