using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Interfaces;

public interface IWorkerService
{
    Task<PageList<WorkerDto>> Get(PageParams pageParams, WorkerFilter filter);
    Task<WorkerDto> GetById(long id, long? companyId = null);
    Task<WorkerDto> Create(long companyId, WorkerCreateDto model);
    Task<WorkerDto> Update(long id, long companyId, WorkerUpdateDto model);
    Task<WorkerDto> Activate(long id, long companyId);
    Task<WorkerDto> Deactivate(long id, long companyId);

    Task<List<WorkerAllocationDto>> GetAllocations(long supplyRequestId, long? companyId = null, bool includeReleased = false);
    Task<WorkerAllocationDto> Allocate(long supplyRequestId, long companyId, WorkerAllocateDto model);
    Task<WorkerAllocationDto> Release(long supplyRequestId, long workerId, long? companyId = null);
}
