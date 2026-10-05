using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IWorkerAllocationRepository : IGenericRepository<WorkerAllocation>
{
    Task<List<WorkerAllocation>> GetBySupplyRequest(long supplyRequestId, bool activeOnly = true);
    Task<List<WorkerAllocation>> GetActiveBySupplyRequests(IReadOnlyCollection<long> supplyRequestIds);
    Task<List<WorkerAllocation>> GetActiveByWorker(long workerId);
    Task<WorkerAllocation?> GetActive(long supplyRequestId, long workerId);
    Task<int> CountActive(long supplyRequestId);
}
