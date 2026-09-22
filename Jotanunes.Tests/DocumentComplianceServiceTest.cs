using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class DocumentComplianceServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<IDocumentTypeRepository> _types = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<ISupplyRequestRepository> _requests = new();
    private readonly Company _company;
    private readonly WorkSite _workSite = new("Obra Teste") { Id = 1 };
    private readonly DocumentType _onboardingType = new("CNPJ", "Cartão CNPJ", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company) { Id = 10 };
    private readonly DocumentType _recurringType = new("FOLHA", "Folha de pagamento", DocumentCategory.Recurring, SupplierType.Material, DocumentSubject.Company) { Id = 20 };
    private readonly DocumentComplianceService _service;

    public DocumentComplianceServiceTest()
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
        _companies.Setup(c => c.GetById(1)).ReturnsAsync(_company);
        _requests.Setup(r => r.GetById(1)).ReturnsAsync(() => Request(1));
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType> { _onboardingType, _recurringType });
        _documents.Setup(d => d.GetAll(It.IsAny<DocumentFilter>())).ReturnsAsync(new List<Document>());
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document>());

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
        var document = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "folha.pdf", "application/pdf", supplyRequestId, null, null, start, end) { Id = 200 };
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
        _documents.Setup(d => d.GetAll(It.IsAny<DocumentFilter>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>()))
            .ReturnsAsync(new List<Document> { ApprovedRecurring(1, period.Start, period.End) });

        var result = await _service.GetPendingBatch(new[] { request });

        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_Ignore_Recurring_Documents_From_Another_Period()
    {
        var request = Request(1);
        var previousMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);
        _documents.Setup(d => d.GetAll(It.IsAny<DocumentFilter>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>()))
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
        var awaitingReview = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "folha.pdf", "application/pdf", 1, null, null, period.Start, period.End) { Id = 300 };
        _documents.Setup(d => d.GetAll(It.IsAny<DocumentFilter>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { awaitingReview });

        var result = await _service.GetPendingBatch(new[] { request });

        Assert.Equal(1, Assert.Single(result).Value.MissingRecurringCompanyCount);
    }

    [Fact]
    public async Task Should_Skip_Closed_Requests()
    {
        var result = await _service.GetPendingBatch(new[] { Request(1, cancelled: true) });

        Assert.Empty(result);
        _documents.Verify(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>()), Times.Never);
    }

    [Fact]
    public async Task Should_Load_Documents_And_Types_Once_For_The_Whole_Batch()
    {
        var requests = new[] { Request(1), Request(2), Request(3) };

        var result = await _service.GetPendingBatch(requests);

        Assert.Equal(3, result.Count);
        _documents.Verify(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>()), Times.Once);
        _documents.Verify(d => d.GetAll(It.IsAny<DocumentFilter>()), Times.Once);
        _types.Verify(t => t.GetApplicable(It.IsAny<SupplierType>()), Times.Once);
    }

    private DocumentType WorkerType(long id, string code) =>
        new(code, code, DocumentCategory.Recurring, SupplierType.Material, DocumentSubject.Worker) { Id = id };

    private Document Worker(DocumentType type, string cpf, long supplyRequestId, DateOnly start, DateOnly end, long id, Action<Document>? review = null)
    {
        var document = new Document(1, type.Id, 1, DocumentCategory.Recurring, DocumentSubject.Worker, "k", "f.pdf", "application/pdf", supplyRequestId, "Cícero", cpf, start, end) { Id = id };
        review?.Invoke(document);
        return document;
    }

    [Fact]
    public async Task Should_Mark_Item_As_Rejected_With_Reason_And_Document_Id()
    {
        var request = Request(1);
        var period = CurrentPeriod();
        var rejected = ApprovedRecurring(1, period.Start, period.End);
        typeof(Document).GetProperty(nameof(Document.Status))!.SetValue(rejected, DocumentStatus.Pending);
        rejected.Reject("Ilegível");
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { rejected });

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

        var rejected = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "a.pdf", "application/pdf", 1, null, null, period.Start, period.End) { Id = 1, CreatedAt = DateTime.UtcNow.AddHours(-3) };
        rejected.Reject("Borrado");
        var resubmitted = new Document(1, _recurringType.Id, 1, DocumentCategory.Recurring, DocumentSubject.Company, "k", "b.pdf", "application/pdf", 1, null, null, period.Start, period.End) { Id = 2, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { rejected, resubmitted });

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
        _documents.Setup(d => d.GetAll(It.IsAny<DocumentFilter>())).ReturnsAsync(new List<Document> { ApprovedOnboarding() });
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { ApprovedRecurring(1, period.Start, period.End) });

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
        var approved = Worker(timesheet, "52998224725", 1, period.Start, period.End, 1, d => d.Approve());
        var rejected = Worker(receipt, "52998224725", 1, period.Start, period.End, 2, d => d.Reject("CPF ilegível"));
        _documents.Setup(d => d.GetBySupplyRequests(It.IsAny<IReadOnlyCollection<long>>())).ReturnsAsync(new List<Document> { approved, rejected });

        var checklist = await _service.GetChecklist(1);

        var worker = Assert.Single(checklist.Workers);
        Assert.False(worker.IsUpToDate);
        Assert.Equal(0, checklist.WorkersUpToDate);
        Assert.Equal(ChecklistItemStatus.Approved, worker.Items.Single(i => i.DocumentTypeCode == "TIMESHEET").Status);
        Assert.Equal("CPF ilegível", worker.Items.Single(i => i.DocumentTypeCode == "RECEIPT").RejectionReason);
    }
}
