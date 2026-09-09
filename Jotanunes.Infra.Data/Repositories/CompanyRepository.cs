using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;
using Jotanunes.Domain.Validation;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PageList<Company>> Get(PageParams pageParams, CompanyFilter filter)
    {
        IQueryable<Company> query = _context.Companies.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.CorporateName))
        {
            query = query.Where(c => c.CorporateName.ToLower().Contains(filter.CorporateName.ToLower()));
        }

        if (!string.IsNullOrEmpty(filter.TradeName))
        {
            query = query.Where(c => c.TradeName.ToLower().Contains(filter.TradeName.ToLower()));
        }

        if (!string.IsNullOrEmpty(filter.Cnpj))
        {
            var cnpj = Cnpj.Normalize(filter.Cnpj);
            query = query.Where(c => c.Cnpj.Contains(cnpj));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query.OrderBy(c => c.CorporateName)
                            .Skip((pageParams.PageNumber - 1) * pageParams.PageSize)
                            .Take(pageParams.PageSize)
                            .ToListAsync();

        return new PageList<Company>(items, totalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<Company?> GetById(long id)
    {
        return await _context.Companies
                                .Include(c => c.Users)
                                .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByCnpj(string cnpj)
    {
        var digits = Cnpj.Normalize(cnpj);
        return await _context.Companies
                                .FirstOrDefaultAsync(c => c.Cnpj == digits);
    }

    public async Task<bool> CnpjInUse(string cnpj, long? ignoreCompanyId = null)
    {
        var digits = Cnpj.Normalize(cnpj);
        return await _context.Companies
                                .AnyAsync(c => c.Cnpj == digits && (!ignoreCompanyId.HasValue || c.Id != ignoreCompanyId.Value));
    }
}
