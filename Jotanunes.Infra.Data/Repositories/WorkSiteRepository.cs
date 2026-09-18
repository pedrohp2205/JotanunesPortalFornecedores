using Jotanunes.Domain.Entities;
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

    public async Task<List<WorkSite>> Get()
    {
        return await _context.WorkSites
                                .AsNoTracking()
                                .OrderBy(w => w.Name)
                                .ToListAsync();
    }

    public async Task<WorkSite?> GetById(long id)
    {
        return await _context.WorkSites
                                .FirstOrDefaultAsync(w => w.Id == id);
    }
}
