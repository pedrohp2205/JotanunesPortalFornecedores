using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IPeriodComplianceReportRepository : IGenericRepository<PeriodComplianceReport>
{
    Task<PeriodComplianceReport?> GetById(long id);
    Task<PeriodComplianceReport?> Get(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd);
    Task<List<PeriodComplianceReport>> GetBySupplyRequest(long supplyRequestId);
    Task<List<PeriodComplianceReport>> GetEndingFrom(long supplyRequestId, DateOnly from);
    Task<List<long>> GetPendingIds(int take);
}
