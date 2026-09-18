using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class SupplyRequestRepository : GenericRepository<SupplyRequest>, ISupplyRequestRepository
{
    private readonly ApplicationDbContext _context;

    public SupplyRequestRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<SupplyRequest>> Get(SupplyRequestFilter filter)
    {
        var query = _context.SupplyRequests
                                .AsNoTracking()
                                .Include(sr => sr.Company)
                                .Include(sr => sr.WorkSite)
                                .AsQueryable();

        if (filter.CompanyId.HasValue)
        {
            query = query.Where(sr => sr.CompanyId == filter.CompanyId.Value);
        }

        if (filter.WorkSiteId.HasValue)
        {
            query = query.Where(sr => sr.WorkSiteId == filter.WorkSiteId.Value);
        }

        if (filter.SupplierType.HasValue)
        {
            query = query.Where(sr => sr.SupplierType == filter.SupplierType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(sr => sr.Status == filter.Status.Value);
        }

        return await query
                        .OrderBy(sr => sr.WorkSite.Name)
                        .ThenBy(sr => sr.Company.CorporateName)
                        .ThenBy(sr => sr.SupplierType)
                        .ToListAsync();
    }

    public async Task<SupplyRequest?> GetById(long id)
    {
        return await _context.SupplyRequests
                                .Include(sr => sr.Company)
                                .Include(sr => sr.WorkSite)
                                .FirstOrDefaultAsync(sr => sr.Id == id);
    }

    public async Task<List<SupplyRequest>> GetActive()
    {
        return await _context.SupplyRequests
                                .AsNoTracking()
                                .Include(sr => sr.Company)
                                .Include(sr => sr.WorkSite)
                                .Where(sr => sr.Status == SupplyRequestStatus.Open || sr.Status == SupplyRequestStatus.InProgress)
                                .OrderBy(sr => sr.Company.CorporateName)
                                .ThenBy(sr => sr.WorkSite.Name)
                                .ToListAsync();
    }

    // Indica se há solicitação ativa da empresa em algum dos tipos informados (aceita flags).
    public async Task<bool> HasActive(long companyId, SupplierType supplierTypes)
    {
        return await _context.SupplyRequests
                                .AnyAsync(sr => sr.CompanyId == companyId
                                             && (sr.SupplierType & supplierTypes) != 0
                                             && (sr.Status == SupplyRequestStatus.Open || sr.Status == SupplyRequestStatus.InProgress));
    }

    public async Task<bool> ActiveExists(long companyId, long workSiteId, SupplierType supplierType)
    {
        return await _context.SupplyRequests
                                .AnyAsync(sr => sr.CompanyId == companyId
                                             && sr.WorkSiteId == workSiteId
                                             && sr.SupplierType == supplierType
                                             && (sr.Status == SupplyRequestStatus.Open || sr.Status == SupplyRequestStatus.InProgress));
    }
}
