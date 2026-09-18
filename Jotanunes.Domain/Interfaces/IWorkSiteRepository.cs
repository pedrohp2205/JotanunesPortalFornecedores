using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkSiteRepository : IGenericRepository<WorkSite>
{
    Task<List<WorkSite>> Get();
    Task<WorkSite?> GetById(long id);
}
