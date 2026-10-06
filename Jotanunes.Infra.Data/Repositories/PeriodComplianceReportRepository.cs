using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class PeriodComplianceReportRepository : GenericRepository<PeriodComplianceReport>, IPeriodComplianceReportRepository
{
    private readonly ApplicationDbContext _context;

    public PeriodComplianceReportRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PeriodComplianceReport?> GetById(long id)
    {
        return await _context.PeriodComplianceReports
                                .Include(r => r.SupplyRequest).ThenInclude(sr => sr.WorkSite)
                                .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<PeriodComplianceReport?> Get(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd)
    {
        return await _context.PeriodComplianceReports
                                .FirstOrDefaultAsync(r => r.SupplyRequestId == supplyRequestId && r.PeriodStart == periodStart && r.PeriodEnd == periodEnd);
    }

    public async Task<List<PeriodComplianceReport>> GetBySupplyRequest(long supplyRequestId)
    {
        return await _context.PeriodComplianceReports
                                .AsNoTracking()
                                .Where(r => r.SupplyRequestId == supplyRequestId)
                                .OrderByDescending(r => r.PeriodStart)
                                .ToListAsync();
    }

    public async Task<List<PeriodComplianceReport>> GetEndingFrom(long supplyRequestId, DateOnly from)
    {
        return await _context.PeriodComplianceReports
                                .Where(r => r.SupplyRequestId == supplyRequestId && r.PeriodEnd >= from)
                                .ToListAsync();
    }

    public async Task<List<long>> GetPendingIds(int take)
    {
        return await _context.PeriodComplianceReports
                                .AsNoTracking()
                                .Where(r => r.Status == PeriodComplianceStatus.Pending)
                                .OrderBy(r => r.RequestedAt)
                                .Select(r => r.Id)
                                .Take(take)
                                .ToListAsync();
    }
}
