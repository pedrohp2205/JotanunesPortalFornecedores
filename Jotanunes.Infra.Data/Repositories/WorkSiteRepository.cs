using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class WorkSiteRepository : GenericRepository<WorkSite>, IWorkSiteRepository
{
    private readonly ApplicationDbContext _context;

    public WorkSiteRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<WorkSite>> Get(WorkSiteFilter filter)
    {
        IQueryable<WorkSite> query = _context.WorkSites.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(name));
        }

        if (filter.RenewalPeriodDays.HasValue)
        {
            query = query.Where(w => w.RenewalPeriodDays == filter.RenewalPeriodDays.Value);
        }

        return await Order(query, filter).ToListAsync();
    }

    public async Task<WorkSite?> GetById(long id)
    {
        return await _context.WorkSites
                                .FirstOrDefaultAsync(w => w.Id == id);
    }

    private static IQueryable<WorkSite> Order(IQueryable<WorkSite> query, WorkSiteFilter filter)
    {
        bool descending = filter.OrderDirection == OrderDirection.Descending;

        IOrderedQueryable<WorkSite> ordered = filter.OrderBy switch
        {
            WorkSiteOrderBy.RenewalPeriodDays => descending ? query.OrderByDescending(w => w.RenewalPeriodDays) : query.OrderBy(w => w.RenewalPeriodDays),
            WorkSiteOrderBy.CreatedAt => descending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt),
            WorkSiteOrderBy.UpdatedAt => descending ? query.OrderByDescending(w => w.UpdatedAt) : query.OrderBy(w => w.UpdatedAt),
            _ => descending ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name)
        };

        return ordered.ThenBy(w => w.Id);
    }
}
