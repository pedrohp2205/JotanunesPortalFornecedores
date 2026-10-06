using AutoMapper;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services.Compliance;

public class PeriodComplianceService : IPeriodComplianceService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PeriodComplianceService(IMapper mapper, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task RequestRecalculation(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd)
    {
        var report = await _unitOfWork.PeriodComplianceReportRepository.Get(supplyRequestId, periodStart, periodEnd);

        if (report is null)
        {
            _unitOfWork.PeriodComplianceReportRepository.Add(new PeriodComplianceReport(supplyRequestId, periodStart, periodEnd, Now));
            return;
        }

        report.RequestRecalculation(Now);
        _unitOfWork.PeriodComplianceReportRepository.Update(report);
    }

    public async Task RequestRecalculationFrom(long supplyRequestId, DateOnly from)
    {
        foreach (var report in await _unitOfWork.PeriodComplianceReportRepository.GetEndingFrom(supplyRequestId, from))
        {
            report.RequestRecalculation(Now);
            _unitOfWork.PeriodComplianceReportRepository.Update(report);
        }
    }

    public async Task<List<long>> GetPendingIds(int take)
    {
        return await _unitOfWork.PeriodComplianceReportRepository.GetPendingIds(take);
    }

    public async Task Recalculate(long reportId, CancellationToken cancellationToken = default)
    {
        var report = await _unitOfWork.PeriodComplianceReportRepository.GetById(reportId);
        if (report is null || report.Status != PeriodComplianceStatus.Pending)
        {
            return;
        }

        var startedAt = Now;
        var documents = await _unitOfWork.DocumentRepository.GetForPeriod(report.SupplyRequestId, report.PeriodStart, report.PeriodEnd);
        var allocations = await _unitOfWork.WorkerAllocationRepository.GetBySupplyRequest(report.SupplyRequestId, activeOnly: false);

        var allocatedWorkers = allocations
            .Where(a => DateOnly.FromDateTime(a.AllocatedAt) <= report.PeriodEnd
                && (a.ReleasedAt is null || DateOnly.FromDateTime(a.ReleasedAt.Value) >= report.PeriodStart))
            .Select(a => a.Worker)
            .DistinctBy(w => w.Id)
            .ToList();

        var context = new PeriodContext(
            report.PeriodStart,
            report.PeriodEnd,
            documents.Select(ToPeriodDocument).ToList(),
            allocatedWorkers);

        cancellationToken.ThrowIfCancellationRequested();

        report.Complete(PeriodComplianceRules.Evaluate(context), startedAt, Now);
        _unitOfWork.PeriodComplianceReportRepository.Update(report);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<PeriodComplianceReportDto>> GetBySupplyRequest(long supplyRequestId)
    {
        await EnsureSupplyRequestExists(supplyRequestId);

        var reports = await _unitOfWork.PeriodComplianceReportRepository.GetBySupplyRequest(supplyRequestId);
        return _mapper.Map<List<PeriodComplianceReportDto>>(reports);
    }

    public async Task<PeriodComplianceReportDto> Recalculate(long supplyRequestId, PeriodComplianceRecalculateDto model)
    {
        await EnsureSupplyRequestExists(supplyRequestId);

        await RequestRecalculation(supplyRequestId, model.PeriodStart, model.PeriodEnd);
        await _unitOfWork.SaveChangesAsync();

        var report = await _unitOfWork.PeriodComplianceReportRepository.Get(supplyRequestId, model.PeriodStart, model.PeriodEnd);
        return _mapper.Map<PeriodComplianceReportDto>(report);
    }

    private async Task EnsureSupplyRequestExists(long supplyRequestId)
    {
        if (await _unitOfWork.SupplyRequestRepository.GetById(supplyRequestId) is null)
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }
    }

    private static PeriodDocument ToPeriodDocument(Document document)
    {
        var analysis = document.Analysis;
        var read = analysis is { Status: DocumentAnalysisStatus.Completed, FoundWrongDocument: false };

        return new PeriodDocument(
            document,
            read ? FieldExtraction.FromFields(analysis!.Fields) : null,
            read ? analysis!.Engine : null);
    }
}
