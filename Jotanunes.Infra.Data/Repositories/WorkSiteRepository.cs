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

    public async Task<List<CompanyWorkSite>> GetByCompany(long companyId)
    {
        return await _context.CompanyWorkSites
                                .AsNoTracking()
                                .Include(cw => cw.WorkSite)
                                .Where(cw => cw.CompanyId == companyId)
                                .OrderBy(cw => cw.WorkSite.Name)
                                .ToListAsync();
    }

    public async Task<List<CompanyWorkSite>> GetCompanies(long workSiteId)
    {
        return await _context.CompanyWorkSites
                                .AsNoTracking()
                                .Include(cw => cw.Company)
                                .Where(cw => cw.WorkSiteId == workSiteId)
                                .OrderBy(cw => cw.Company.CorporateName)
                                .ToListAsync();
    }

    public async Task<CompanyWorkSite?> GetLink(long workSiteId, long companyId)
    {
        return await _context.CompanyWorkSites
                                .FirstOrDefaultAsync(cw => cw.WorkSiteId == workSiteId && cw.CompanyId == companyId);
    }

    public async Task<CompanyWorkSite?> GetLinkById(long id)
    {
        return await _context.CompanyWorkSites
                                .Include(cw => cw.WorkSite)
                                .FirstOrDefaultAsync(cw => cw.Id == id);
    }

    public void AddLink(CompanyWorkSite link)
    {
        _context.CompanyWorkSites.Add(link);
    }

    public void RemoveLink(CompanyWorkSite link)
    {
        _context.CompanyWorkSites.Remove(link);
    }
}
