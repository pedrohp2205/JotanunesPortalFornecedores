using AutoMapper;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Services.Compliance;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jotanunes.Tests.Application.Services.Compliance;

public class PeriodComplianceServiceTests
{
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateOnly End = new(2026, 7, 31);
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPeriodComplianceReportRepository> _reports = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<IWorkerAllocationRepository> _allocations = new();
    private readonly Mock<ISupplyRequestRepository> _requests = new();
    private readonly PeriodComplianceService _service;

    public PeriodComplianceServiceTests()
    {
        _unitOfWork.SetupGet(u => u.PeriodComplianceReportRepository).Returns(_reports.Object);
        _unitOfWork.SetupGet(u => u.DocumentRepository).Returns(_documents.Object);
        _unitOfWork.SetupGet(u => u.WorkerAllocationRepository).Returns(_allocations.Object);
        _unitOfWork.SetupGet(u => u.SupplyRequestRepository).Returns(_requests.Object);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        _service = new PeriodComplianceService(mapper, _unitOfWork.Object, new FixedTimeProvider(Now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static WorkerAllocation Allocation(Worker worker, DateTime allocatedAt, DateTime? releasedAt)
    {
        var request = new SupplyRequest(1, 9, SupplierType.ManpowerLabor) { Id = 3 };
        var allocation = new WorkerAllocation(request, worker, 0);
        typeof(WorkerAllocation).GetProperty(nameof(WorkerAllocation.AllocatedAt))!.SetValue(allocation, allocatedAt);
        typeof(WorkerAllocation).GetProperty(nameof(WorkerAllocation.ReleasedAt))!.SetValue(allocation, releasedAt);
        return allocation;
    }

    [Fact]
    public async Task Should_Create_Pending_Report_When_None_Exists()
    {
        PeriodComplianceReport? added = null;
        _reports.Setup(r => r.Add(It.IsAny<PeriodComplianceReport>())).Callback<PeriodComplianceReport>(r => added = r);

        await _service.RequestRecalculation(3, Start, End);

        Assert.NotNull(added);
        Assert.Equal(PeriodComplianceStatus.Pending, added.Status);
        Assert.Equal(Start, added.PeriodStart);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Put_Existing_Report_Back_In_The_Queue()
    {
        var report = new PeriodComplianceReport(3, Start, End, Now.UtcDateTime.AddDays(-1));
        report.Complete([], Now.UtcDateTime.AddDays(-1), Now.UtcDateTime.AddDays(-1));
        _reports.Setup(r => r.Get(3, Start, End)).ReturnsAsync(report);

        await _service.RequestRecalculation(3, Start, End);

        Assert.Equal(PeriodComplianceStatus.Pending, report.Status);
        _reports.Verify(r => r.Add(It.IsAny<PeriodComplianceReport>()), Times.Never);
    }

    [Fact]
    public async Task Should_Cross_Documents_With_Workers_Allocated_During_The_Period()
    {
        var report = new PeriodComplianceReport(3, Start, End, Now.UtcDateTime.AddMinutes(-1)) { Id = 50 };
        _reports.Setup(r => r.GetById(50)).ReturnsAsync(report);

        var maria = new Worker(1, AnalysisTestData.WorkerName, AnalysisTestData.WorkerCpf) { Id = 7 };
        var releasedBefore = new Worker(1, "Joao Pereira Lima", "111.444.777-35") { Id = 8 };
        var allocatedAfter = new Worker(1, "Ana Paula Souza", "390.533.447-05") { Id = 9 };
        _allocations.Setup(a => a.GetBySupplyRequest(3, false)).ReturnsAsync([
            Allocation(maria, new DateTime(2026, 6, 1), null),
            Allocation(releasedBefore, new DateTime(2026, 5, 1), new DateTime(2026, 6, 20)),
            Allocation(allocatedAfter, new DateTime(2026, 8, 5), null)
        ]);

        var fgtsDetail = AnalysisTestData.RecurringDocument("FGTS_DETAIL", "Detalhamento do FGTS", DocumentSubject.Company);
        var analysis = new DocumentAnalysis(fgtsDetail);
        analysis.Complete(TextExtractionEngine.NativeText, [new ExtractedField("workers", "[]")], []);
        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(fgtsDetail, analysis);
        _documents.Setup(d => d.GetForPeriod(3, Start, End)).ReturnsAsync([fgtsDetail]);

        await _service.Recalculate(50);

        Assert.Equal(PeriodComplianceStatus.Completed, report.Status);
        var finding = Assert.Single(report.Findings);
        Assert.Equal("WORKER_WITHOUT_FGTS", finding.Code);
        Assert.Contains("Maria Aparecida dos Santos", finding.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Treat_Wrong_Document_As_Not_Read()
    {
        var report = new PeriodComplianceReport(3, Start, End, Now.UtcDateTime.AddMinutes(-1)) { Id = 50 };
        _reports.Setup(r => r.GetById(50)).ReturnsAsync(report);
        _allocations.Setup(a => a.GetBySupplyRequest(3, false)).ReturnsAsync([]);

        var guide = AnalysisTestData.RecurringDocument("FGTS_REPORT", "Relatório do FGTS", DocumentSubject.Company);
        var analysis = new DocumentAnalysis(guide);
        analysis.Complete(TextExtractionEngine.Vision, [], [new AnalysisFinding(DocumentAnalysis.WrongDocumentTypeCode, FindingSeverity.Blocking, "CNH")]);
        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(guide, analysis);
        _documents.Setup(d => d.GetForPeriod(3, Start, End)).ReturnsAsync([guide]);

        await _service.Recalculate(50);

        Assert.Equal("UNREAD_DOCUMENTS", Assert.Single(report.Findings).Code);
    }

    [Fact]
    public async Task Should_Ignore_Report_That_Is_Not_Pending()
    {
        var report = new PeriodComplianceReport(3, Start, End, Now.UtcDateTime.AddDays(-1)) { Id = 50 };
        report.Complete([], Now.UtcDateTime.AddDays(-1), Now.UtcDateTime.AddDays(-1));
        _reports.Setup(r => r.GetById(50)).ReturnsAsync(report);

        await _service.Recalculate(50);

        _documents.Verify(d => d.GetForPeriod(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task Should_Refuse_Recalculation_Of_Unknown_Supply_Request()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.Recalculate(99, new PeriodComplianceRecalculateDto { PeriodStart = Start, PeriodEnd = End }));
    }
}
