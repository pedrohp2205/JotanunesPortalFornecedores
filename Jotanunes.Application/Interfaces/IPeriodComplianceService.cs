using Jotanunes.Application.DTOs.Compliance;

namespace Jotanunes.Application.Interfaces;

public interface IPeriodComplianceService
{
    Task RequestRecalculation(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd);
    Task RequestRecalculationFrom(long supplyRequestId, DateOnly from);
    Task<List<long>> GetPendingIds(int take);
    Task Recalculate(long reportId, CancellationToken cancellationToken = default);
    Task<List<PeriodComplianceReportDto>> GetBySupplyRequest(long supplyRequestId);
    Task<PeriodComplianceReportDto> Recalculate(long supplyRequestId, PeriodComplianceRecalculateDto model);
}
