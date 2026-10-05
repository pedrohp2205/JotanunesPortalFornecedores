using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests.Application.Services;

public class DocumentComplianceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<IDocumentTypeRepository> _types = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<ISupplyRequestRepository> _requests = new();
    private readonly Mock<IWorkerAllocationRepository> _allocations = new();
    private readonly Company _company;
    private readonly WorkSite _workSite = new("Obra Teste") { Id = 1 };
    private readonly DocumentType _onboardingType = new("CNPJ", "Cartão CNPJ", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company) { Id = 10 };
    private readonly DocumentType _recurringType = new("FOLHA", "Folha de pagamento", DocumentCategory.Recurring, SupplierType.Material, DocumentSubject.Company) { Id = 20 };
    private readonly DocumentComplianceService _service;

    public DocumentComplianceServiceTests()
    {
        _company = new Company(
            "11.222.333/0001-81",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000", "Sala 2"),
            SupplierType.Material)
        { Id = 1 };

        _unitOfWork.SetupGet(u => u.DocumentRepository).Returns(_documents.Object);
        _unitOfWork.SetupGet(u => u.DocumentTypeRepository).Returns(_types.Object);
        _unitOfWork.SetupGet(u => u.CompanyRepository).Returns(_companies.Object);
        _unitOfWork.SetupGet(u => u.SupplyRequestRepository).Returns(_requests.Object);
        _unitOfWork.SetupGet(u => u.WorkerAllocationRepository).Returns(_allocations.Object);
        _allocations.Setup(a => a.GetActiveBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<WorkerAllocation>());
        _documents.Setup(d => d.GetOnboardingByWorkers(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document>());
        _companies.Setup(c => c.GetById(1)).ReturnsAsync(_company);
        _requests.Setup(r => r.GetById(1)).ReturnsAsync(() => Request(1));
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { _onboardingType, _recurringType });
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document>());
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document>());

        _service = new DocumentComplianceService(_unitOfWork.Object);
    }

    private SupplyRequest Request(long id, bool cancelled = false)
    {
        var request = new SupplyRequest(_company, 1, SupplierType.Material) { Id = id };
        typeof(SupplyRequest).GetProperty(nameof(SupplyRequest.WorkSite))!.SetValue(request, _workSite);

        if (cancelled)
        {
            request.Cancel();
        }

        return request;
    }

    private Document ApprovedOnboarding()
    {
        var document = new Document(1, _onboardingType.Id, 1, DocumentCategory.Onboarding, DocumentSubject.Company, "k", "cnpj.pdf", "application/pdf") { Id = 100 };
        document.Approve();
        return document;
    }

    private Document ApprovedRecurring(long supplyRequestId, DateOnly start, DateOnly end)
    {
        var document = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "folha.pdf", "application/pdf", supplyRequestId, null, start, end) { Id = 200 };
        document.Approve();
        return document;
    }

    private (DateOnly Start, DateOnly End) CurrentPeriod()
    {
        return _workSite.GetCurrentPeriod(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task Should_Report_Missing_Items_For_An_Active_Request()
    {
        var request = Request(1);

        var result = await _service.GetPendingBatch(new[] { request });

        var pending = Assert.Single(result).Value;
        Assert.Equal(1, pending.MissingOnboardingCount);
        Assert.Equal(1, pending.MissingRecurringCompanyCount);
        Assert.Equal(CurrentPeriod().Start, pending.PeriodStart);
        Assert.Equal(CurrentPeriod().End, pending.PeriodEnd);
    }

    [Fact]
    public async Task Should_Omit_Request_When_Everything_Is_Approved_In_The_Current_Period()
    {
        var request = Request(1);
        var period = CurrentPeriod();
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(new List<Document> { ApprovedRecurring(1, period.Start, period.End) });

        var result = await _service.GetPendingBatch(new[] { request });

        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_Ignore_Recurring_Documents_From_Another_Period()
    {
        var request = Request(1);
        var previousMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(new List<Document> { ApprovedRecurring(1, previousMonth.AddDays(-5), previousMonth) });

        var result = await _service.GetPendingBatch(new[] { request });

        var pending = Assert.Single(result).Value;
        Assert.Equal(0, pending.MissingOnboardingCount);
        Assert.Equal(1, pending.MissingRecurringCompanyCount);
    }

    [Fact]
    public async Task Should_Not_Count_Documents_Still_Waiting_For_Review()
    {
        var request = Request(1);
        var period = CurrentPeriod();
        var awaitingReview = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "folha.pdf", "application/pdf", 1, null, period.Start, period.End) { Id = 300 };
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document> { awaitingReview });

        var result = await _service.GetPendingBatch(new[] { request });

        Assert.Equal(1, Assert.Single(result).Value.MissingRecurringCompanyCount);
    }

    [Fact]
    public async Task Should_Skip_Closed_Requests()
    {
        var result = await _service.GetPendingBatch(new[] { Request(1, cancelled: true) });

        Assert.Empty(result);
        _documents.Verify(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()), Times.Never);
    }

    [Fact]
    public async Task Should_Load_Documents_And_Types_Once_For_The_Whole_Batch()
    {
        var requests = new[] { Request(1), Request(2), Request(3) };

        var result = await _service.GetPendingBatch(requests);

        Assert.Equal(3, result.Count);
        _documents.Verify(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()), Times.Once);
        _documents.Verify(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>()), Times.Once);
        _types.Verify(t => t.GetApplicable(It.IsAny<SupplierType>()), Times.Once);
    }

    [Fact]
    public async Task Should_Load_Onboarding_Of_All_Companies_In_A_Single_Query()
    {
        var otherCompany = new Company(
            "11.444.777/0001-61",
            "Outra Empresa LTDA",
            "Outra",
            "contato@outra.com.br",
            "(79) 98888-7777",
            "João Lima",
            new Address("Rua B", "1", "Centro", "Aracaju", "SE", "49000-000"),
            SupplierType.Material)
        { Id = 2 };
        var otherRequest = new SupplyRequest(otherCompany, 1, SupplierType.Material) { Id = 2 };
        typeof(SupplyRequest).GetProperty(nameof(SupplyRequest.WorkSite))!.SetValue(otherRequest, _workSite);

        await _service.GetPendingBatch(new[] { Request(1), otherRequest });

        _documents.Verify(d => d.GetOnboardingByCompanies(It.Is<IReadOnlyCollection<long>>(ids => ids.OrderBy(id => id).SequenceEqual(new long[] { 1, 2 }))), Times.Once);
    }

    [Fact]
    public async Task Should_Load_Only_Recurring_Documents_Reaching_The_Current_Period()
    {
        await _service.GetPendingBatch(new[] { Request(1) });

        _documents.Verify(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), CurrentPeriod().Start), Times.Once);
    }

    private DocumentType WorkerType(long id, string code, DocumentCategory category = DocumentCategory.Recurring) =>
        new(code, code, category, SupplierType.ManpowerLabor, DocumentSubject.Worker) { Id = id };

    private Document WorkerDocument(DocumentType type, long workerId, long supplyRequestId, DateOnly start, DateOnly end, long id, Action<Document>? review = null)
    {
        var document = new Document(1, type.Id, 1, DocumentCategory.Recurring, DocumentSubject.Worker, "k", "f.pdf", "application/pdf", supplyRequestId, workerId, start, end) { Id = id };
        review?.Invoke(document);
        return document;
    }

    private Document WorkerOnboardingDocument(DocumentType type, long workerId, DateOnly expirationDate, long id)
    {
        var document = new Document(1, type.Id, 1, DocumentCategory.Onboarding, DocumentSubject.Worker, "k", "aso.pdf", "application/pdf", workerId: workerId, expirationDate: expirationDate) { Id = id };
        document.Approve();
        return document;
    }

    private SupplyRequest ManpowerRequest(long id, int? requiredWorkerCount = 2)
    {
        var request = new SupplyRequest(_company, 1, SupplierType.ManpowerLabor, requiredWorkerCount) { Id = id };
        typeof(SupplyRequest).GetProperty(nameof(SupplyRequest.WorkSite))!.SetValue(request, _workSite);
        _requests.Setup(r => r.GetById(id)).ReturnsAsync(request);
        return request;
    }

    private void Allocate(SupplyRequest request, params Worker[] workers)
    {
        var allocations = workers.Select((w, i) => new WorkerAllocation(request, w, i)).ToList();
        _allocations.Setup(a => a.GetActiveBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(allocations);
    }

    private static Worker Cicero() => new(1, "Cícero", "529.982.247-25") { Id = 7 };
    private static Worker Josefa() => new(1, "Josefa", "111.444.777-35") { Id = 8 };

    [Fact]
    public async Task Should_Mark_Item_As_Rejected_With_Reason_And_Document_Id()
    {
        var request = Request(1);
        var period = CurrentPeriod();
        var rejected = ApprovedRecurring(1, period.Start, period.End);
        typeof(Document).GetProperty(nameof(Document.Status))!.SetValue(rejected, DocumentStatus.Pending);
        rejected.Reject("Ilegível");
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document> { rejected });

        var checklist = await _service.GetChecklist(1);

        var item = Assert.Single(checklist.RecurringCompanyItems);
        Assert.Equal(ChecklistItemStatus.Rejected, item.Status);
        Assert.Equal("Rejected", item.StatusDescription);
        Assert.Equal("Ilegível", item.RejectionReason);
        Assert.Equal(rejected.Id, item.DocumentId);
        Assert.False(item.IsSatisfied);
    }

    [Fact]
    public async Task Should_Prefer_Pending_Resubmission_Over_Older_Rejection_And_Approved_Over_Both()
    {
        var period = CurrentPeriod();

        var rejected = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "a.pdf", "application/pdf", 1, null, period.Start, period.End) { Id = 1, CreatedAt = DateTime.UtcNow.AddHours(-3) };
        rejected.Reject("Borrado");
        var resubmitted = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "b.pdf", "application/pdf", 1, null, period.Start, period.End) { Id = 2, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document> { rejected, resubmitted });

        var whilePending = Assert.Single((await _service.GetChecklist(1)).RecurringCompanyItems);
        Assert.Equal(ChecklistItemStatus.Pending, whilePending.Status);
        Assert.Equal(2, whilePending.DocumentId);
        Assert.Null(whilePending.RejectionReason);

        resubmitted.Approve();

        var afterApproval = Assert.Single((await _service.GetChecklist(1)).RecurringCompanyItems);
        Assert.Equal(ChecklistItemStatus.Approved, afterApproval.Status);
        Assert.True(afterApproval.IsSatisfied);
    }

    [Fact]
    public async Task Should_Report_Not_Sent_When_There_Is_No_Document()
    {
        var checklist = await _service.GetChecklist(1);

        Assert.All(checklist.OnboardingItems.Concat(checklist.RecurringCompanyItems), i =>
        {
            Assert.Equal(ChecklistItemStatus.NotSent, i.Status);
            Assert.Null(i.DocumentId);
        });
    }

    [Fact]
    public async Task Should_Show_Conditional_Types_As_Optional_Without_Counting_As_Pending()
    {
        var simples = new DocumentType("SIMPLES", "Comprovante do Simples", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company, isConditional: true, conditionDescription: "Optante do Simples Nacional") { Id = 30 };
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { simples, _onboardingType, _recurringType });
        var period = CurrentPeriod();
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document> { ApprovedRecurring(1, period.Start, period.End) });

        var checklist = await _service.GetChecklist(1);

        Assert.Equal(new[] { "CNPJ", "SIMPLES" }, checklist.OnboardingItems.Select(i => i.DocumentTypeCode));
        var optional = checklist.OnboardingItems.Single(i => i.DocumentTypeCode == "SIMPLES");
        Assert.False(optional.IsRequired);
        Assert.Equal("Optante do Simples Nacional", optional.ConditionDescription);
        Assert.Equal(ChecklistItemStatus.NotSent, optional.Status);

        Assert.Empty(await _service.GetPendingBatch(new[] { Request(1) }));
        Assert.True(await _service.IsOnboardingComplete(1));
    }

    [Fact]
    public async Task Should_Not_Count_A_Worker_Without_All_Required_Documents_As_Up_To_Date()
    {
        var timesheet = WorkerType(40, "TIMESHEET");
        var receipt = WorkerType(41, "RECEIPT");
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { _onboardingType, _recurringType, timesheet, receipt });
        var period = CurrentPeriod();
        Allocate(ManpowerRequest(1), Cicero());
        var approved = WorkerDocument(timesheet, 7, 1, period.Start, period.End, 1, d => d.Approve());
        var rejected = WorkerDocument(receipt, 7, 1, period.Start, period.End, 2, d => d.Reject("CPF ilegível"));
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<Document> { approved, rejected });

        var checklist = await _service.GetChecklist(1);

        var worker = Assert.Single(checklist.Workers);
        Assert.False(worker.IsUpToDate);
        Assert.Equal(0, checklist.WorkersUpToDate);
        Assert.Equal(ChecklistItemStatus.Approved, worker.Items.Single(i => i.DocumentTypeCode == "TIMESHEET").Status);
        Assert.Equal("CPF ilegível", worker.Items.Single(i => i.DocumentTypeCode == "RECEIPT").RejectionReason);
    }

    [Fact]
    public async Task Should_List_Allocated_Workers_Even_Without_Documents()
    {
        var timesheet = WorkerType(40, "TIMESHEET");
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { timesheet });
        Allocate(ManpowerRequest(1), Cicero(), Josefa());

        var checklist = await _service.GetChecklist(1);

        Assert.Equal(new[] { "Cícero", "Josefa" }, checklist.Workers.Select(w => w.WorkerName));
        Assert.Equal(2, checklist.AllocatedWorkerCount);
        Assert.All(checklist.Workers, w =>
        {
            Assert.False(w.IsUpToDate);
            Assert.Equal(ChecklistItemStatus.NotSent, Assert.Single(w.Items).Status);
        });
    }

    [Fact]
    public async Task Should_Ignore_Documents_Of_Workers_No_Longer_Allocated()
    {
        var timesheet = WorkerType(40, "TIMESHEET");
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { timesheet });
        var period = CurrentPeriod();
        Allocate(ManpowerRequest(1), Josefa());
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(new List<Document> { WorkerDocument(timesheet, 7, 1, period.Start, period.End, 1, d => d.Approve()) });

        var checklist = await _service.GetChecklist(1);

        var worker = Assert.Single(checklist.Workers);
        Assert.Equal(8, worker.WorkerId);
        Assert.Equal(0, checklist.WorkersUpToDate);
    }

    [Fact]
    public async Task Should_Reuse_Worker_Onboarding_Document_Sent_Outside_The_Request()
    {
        var aso = WorkerType(50, "ASO", DocumentCategory.Onboarding);
        var timesheet = WorkerType(40, "TIMESHEET");
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { aso, timesheet });
        var period = CurrentPeriod();
        Allocate(ManpowerRequest(1), Cicero());
        _documents.Setup(d => d.GetOnboardingByWorkers(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new List<Document> { WorkerOnboardingDocument(aso, 7, DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6), 1) });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(new List<Document> { WorkerDocument(timesheet, 7, 1, period.Start, period.End, 2, d => d.Approve()) });

        var checklist = await _service.GetChecklist(1);

        var worker = Assert.Single(checklist.Workers);
        Assert.Equal(ChecklistItemStatus.Approved, Assert.Single(worker.OnboardingItems).Status);
        Assert.True(worker.IsUpToDate);
        Assert.Equal(1, checklist.WorkersUpToDate);
    }

    [Fact]
    public async Task Should_Mark_Expired_Worker_Onboarding_Document_As_Not_Satisfied()
    {
        var aso = WorkerType(50, "ASO", DocumentCategory.Onboarding);
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { aso });
        Allocate(ManpowerRequest(1), Cicero());
        _documents.Setup(d => d.GetOnboardingByWorkers(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new List<Document> { WorkerOnboardingDocument(aso, 7, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), 1) });

        var checklist = await _service.GetChecklist(1);

        var item = Assert.Single(Assert.Single(checklist.Workers).OnboardingItems);
        Assert.Equal(ChecklistItemStatus.Expired, item.Status);
        Assert.False(item.IsSatisfied);
        Assert.Equal(1, item.DocumentId);
    }

    [Fact]
    public async Task Should_Not_Require_Worker_Onboarding_Types_For_Company_Onboarding()
    {
        var aso = WorkerType(50, "ASO", DocumentCategory.Onboarding);
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { _onboardingType, aso });
        _documents.Setup(d => d.GetOnboardingByCompanies(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });

        Assert.True(await _service.IsOnboardingComplete(1));
        Assert.Equal(new[] { "CNPJ" }, (await _service.GetChecklist(1)).OnboardingItems.Select(i => i.DocumentTypeCode));
    }
}
