using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkerRepository : IGenericRepository<Worker>
{
    Task<PageList<Worker>> Get(PageParams pageParams, WorkerFilter filter);
    Task<Worker?> GetById(long id);
    Task<bool> CpfInUse(long companyId, string cpf);
}
