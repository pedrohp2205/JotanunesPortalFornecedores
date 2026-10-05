using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class WorkerAllocationRepository : GenericRepository<WorkerAllocation>, IWorkerAllocationRepository
{
    private readonly ApplicationDbContext _context;

    public WorkerAllocationRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<WorkerAllocation>> GetBySupplyRequest(long supplyRequestId, bool activeOnly = true)
    {
        var query = _context.WorkerAllocations
                                .AsNoTracking()
                                .Include(a => a.Worker)
                                .Where(a => a.SupplyRequestId == supplyRequestId);

        if (activeOnly)
        {
            query = query.Where(a => a.ReleasedAt == null);
        }

        return await query.OrderBy(a => a.Worker.Name)
                            .ThenByDescending(a => a.AllocatedAt)
                            .ToListAsync();
    }

    public async Task<List<WorkerAllocation>> GetActiveBySupplyRequests(IReadOnlyCollection<long> supplyRequestIds)
    {
        return await _context.WorkerAllocations
                                .AsNoTracking()
                                .Include(a => a.Worker)
                                .Where(a => a.ReleasedAt == null && supplyRequestIds.Contains(a.SupplyRequestId))
                                .ToListAsync();
    }

    public async Task<List<WorkerAllocation>> GetActiveByWorker(long workerId)
    {
        return await _context.WorkerAllocations
                                .Include(a => a.SupplyRequest).ThenInclude(sr => sr.WorkSite)
                                .Where(a => a.ReleasedAt == null && a.WorkerId == workerId)
                                .ToListAsync();
    }

    public async Task<WorkerAllocation?> GetActive(long supplyRequestId, long workerId)
    {
        return await _context.WorkerAllocations
                                .Include(a => a.Worker)
                                .FirstOrDefaultAsync(a => a.ReleasedAt == null
                                                       && a.SupplyRequestId == supplyRequestId
                                                       && a.WorkerId == workerId);
    }

    public async Task<int> CountActive(long supplyRequestId)
    {
        return await _context.WorkerAllocations
                                .CountAsync(a => a.ReleasedAt == null && a.SupplyRequestId == supplyRequestId);
    }
}
