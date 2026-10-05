using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;
using Jotanunes.Domain.Validation;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class WorkerRepository : GenericRepository<Worker>, IWorkerRepository
{
    private readonly ApplicationDbContext _context;

    public WorkerRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PageList<Worker>> Get(PageParams pageParams, WorkerFilter filter)
    {
        var query = _context.Workers
                                .AsNoTracking()
                                .Include(w => w.Company)
                                .AsQueryable();

        if (filter.CompanyId.HasValue)
        {
            query = query.Where(w => w.CompanyId == filter.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(w => w.Name.Contains(filter.Name.Trim()));
        }

        var cpf = Cpf.Normalize(filter.Cpf);
        if (!string.IsNullOrEmpty(cpf))
        {
            query = query.Where(w => w.Cpf.Contains(cpf));
        }

        if (filter.Active.HasValue)
        {
            query = query.Where(w => w.Active == filter.Active.Value);
        }

        if (filter.SupplyRequestId.HasValue)
        {
            query = query.Where(w => _context.WorkerAllocations.Any(a => a.WorkerId == w.Id
                                                                      && a.SupplyRequestId == filter.SupplyRequestId.Value
                                                                      && a.ReleasedAt == null));
        }

        var totalCount = await query.CountAsync();

        var items = await query.OrderBy(w => w.Name)
                            .ThenBy(w => w.Id)
                            .Skip((pageParams.PageNumber - 1) * pageParams.PageSize)
                            .Take(pageParams.PageSize)
                            .ToListAsync();

        return new PageList<Worker>(items, totalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<Worker?> GetById(long id)
    {
        return await _context.Workers
                                .Include(w => w.Company)
                                .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<bool> CpfInUse(long companyId, string cpf)
    {
        var normalized = Cpf.Normalize(cpf);
        return await _context.Workers
                                .AnyAsync(w => w.CompanyId == companyId && w.Cpf == normalized);
    }
}
