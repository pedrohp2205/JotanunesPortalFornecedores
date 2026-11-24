using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkSiteRepository : IGenericRepository<WorkSite>
{
    Task<List<WorkSite>> Get(WorkSiteFilter filter);
    Task<WorkSite?> GetById(long id);
}
