using AutoMapper;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Exceptions;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Projections;
using Jotanunes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jotanunes.Tests.Application.Services;

public class DocumentAnalysisServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDocumentAnalysisRepository> _analyses = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    public DocumentAnalysisServiceTests()
    {
        _unitOfWork.SetupGet(u => u.DocumentAnalysisRepository).Returns(_analyses.Object);
        _unitOfWork.SetupGet(u => u.DocumentRepository).Returns(_documents.Object);
        _storage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream("%PDF-1.7"u8.ToArray()));
    }

    private readonly Mock<IDocumentRasterizer> _rasterizer = new();
    private readonly Mock<ISupplierNotificationService> _notifications = new();
    private readonly Mock<IPeriodComplianceService> _periodCompliance = new();

    private DocumentAnalysisService Service(params IDocumentTextExtractor[] extractors)
    {
        return Service(null, extractors);
    }

    private DocumentAnalysisService Service(IVisionClient? vision, params IDocumentTextExtractor[] extractors)
    {
        _rasterizer.Setup(r => r.Render(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DocumentImage([0x89, 0x50, 0x4E, 0x47], "image/png")]);

        return new DocumentAnalysisService(
            _mapper,
            _unitOfWork.Object,
            _storage.Object,
            extractors,
            [new CrfAnalyzer(), new PaymentReceiptAnalyzer()],
            _rasterizer.Object,
            _notifications.Object,
            _periodCompliance.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<DocumentAnalysisService>.Instance,
            vision);
    }

    private static Mock<IVisionClient> Vision(string json)
    {
        var vision = new Mock<IVisionClient>();
        vision.Setup(v => v.ExtractJson(It.IsAny<VisionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(json);
        return vision;
    }

    private static IDocumentTextExtractor Extractor(TextExtractionEngine engine, string text)
    {
        var extractor = new Mock<IDocumentTextExtractor>();
        extractor.SetupGet(e => e.Engine).Returns(engine);
        extractor.Setup(e => e.Extract(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentText([text]));
        return extractor.Object;
    }

    private DocumentAnalysis PendingAnalysis(string typeCode = "FGTS_CND", DateOnly? expirationDate = null)
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.CrfDocument(expirationDate, typeCode)) { Id = 7 };
        _analyses.Setup(a => a.GetById(7)).ReturnsAsync(analysis);
        return analysis;
    }

    [Fact]
    public async Task Should_Stop_At_First_Engine_That_Reads_All_Required_Fields()
    {
        var analysis = PendingAnalysis();
        var vision = new Mock<IDocumentTextExtractor>();
        vision.SetupGet(e => e.Engine).Returns(TextExtractionEngine.Vision);

        await Service(vision.Object, Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        Assert.Equal(TextExtractionEngine.NativeText, analysis.Engine);
        vision.Verify(e => e.Extract(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Fall_Back_To_Next_Engine_When_Text_Layer_Is_Empty()
    {
        var analysis = PendingAnalysis();

        await Service(
            Extractor(TextExtractionEngine.NativeText, ""),
            Extractor(TextExtractionEngine.Ocr, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        Assert.Equal(TextExtractionEngine.Ocr, analysis.Engine);
        Assert.Equal(AnalysisVerdict.Conforming, analysis.Verdict);
    }

    [Fact]
    public async Task Should_Fall_Back_When_Cnpj_Check_Digit_Fails()
    {
        var analysis = PendingAnalysis();

        await Service(
            Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText(cnpj: "11.222.333/0001-82")),
            Extractor(TextExtractionEngine.Ocr, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(TextExtractionEngine.Ocr, analysis.Engine);
        Assert.Contains(analysis.Fields, f => f.Name == "cnpj" && f.Value == "11222333000181");
    }

    [Fact]
    public async Task Should_Require_Manual_Review_With_Best_Partial_Reading_When_No_Engine_Completes()
    {
        var analysis = PendingAnalysis();

        await Service(
            Extractor(TextExtractionEngine.NativeText, ""),
            Extractor(TextExtractionEngine.Ocr, "Certificado de Regularidade do FGTS Inscrição: 11.222.333/0001-81")).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.ManualReviewRequired, analysis.Status);
        Assert.Equal(TextExtractionEngine.Ocr, analysis.Engine);
        Assert.Null(analysis.Verdict);
        Assert.Contains("início da validade", analysis.FailureReason);
        Assert.Contains(analysis.Fields, f => f.Name == "cnpj");
    }

    [Fact]
    public async Task Should_Mark_Not_Supported_When_No_Analyzer_Handles_The_Type()
    {
        var analysis = PendingAnalysis(typeCode: "SOCIAL_CONTRACT");

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.NotSupported, analysis.Status);
        _storage.Verify(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Keep_Pending_And_Count_Attempt_When_Storage_Fails()
    {
        var analysis = PendingAnalysis();
        _storage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("bucket fora do ar"));

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.Pending, analysis.Status);
        Assert.Equal(1, analysis.Attempts);
        Assert.Equal("bucket fora do ar", analysis.FailureReason);
        Assert.Equal(new DateTime(2026, 7, 20, 12, 0, 30, DateTimeKind.Utc), analysis.NextAttemptAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Read_With_Vision_When_Text_Levels_Cannot_Read_The_Document()
    {
        var analysis = PendingAnalysis();
        var vision = Vision(AnalysisTestData.CrfJson());

        await Service(vision.Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        Assert.Equal(TextExtractionEngine.Vision, analysis.Engine);
        Assert.Contains(analysis.Fields, f => f.Name == "validUntil" && f.Value == "09/08/2026");
        vision.Verify(v => v.ExtractJson(
            It.Is<VisionRequest>(r => r.SchemaName == "crf_fgts" && r.Images.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Call_Vision_When_Text_Is_Enough()
    {
        PendingAnalysis();
        var vision = Vision(AnalysisTestData.CrfJson());

        await Service(vision.Object, Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        vision.Verify(v => v.ExtractJson(It.IsAny<VisionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Report_Wrong_Document_Detected_By_Vision_As_Warning()
    {
        var analysis = PendingAnalysis();
        var vision = Vision(AnalysisTestData.CrfJson(isExpectedDocument: false, detectedDocument: "CNH"));

        await Service(vision.Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(7);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        var finding = Assert.Single(analysis.Findings);
        Assert.Equal("WRONG_DOCUMENT_TYPE", finding.Code);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("CNH", finding.Message);
        Assert.Equal(AnalysisVerdict.NeedsAttention, analysis.Verdict);
    }

    [Fact]
    public async Task Should_Analyze_Payment_Receipt_End_To_End()
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.ReceiptDocument()) { Id = 8 };
        _analyses.Setup(a => a.GetById(8)).ReturnsAsync(analysis);

        await Service(Vision(AnalysisTestData.ReceiptJson()).Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(8);

        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
        Assert.Equal(TextExtractionEngine.Vision, analysis.Engine);
        Assert.Equal("NOT_SIGNED", Assert.Single(analysis.Findings).Code);
        Assert.Contains(analysis.Fields, f => f.Name == "netPay" && f.Value == "1900.75");
    }

    [Fact]
    public async Task Should_Require_Manual_Review_When_Vision_Is_Not_Configured()
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.ReceiptDocument()) { Id = 8 };
        _analyses.Setup(a => a.GetById(8)).ReturnsAsync(analysis);

        await Service(Extractor(TextExtractionEngine.NativeText, "")).Analyze(8);

        Assert.Equal(DocumentAnalysisStatus.ManualReviewRequired, analysis.Status);
    }

    [Fact]
    public async Task Should_Schedule_Retry_Without_Spending_Attempts_When_Vision_Is_Temporarily_Unavailable()
    {
        var analysis = PendingAnalysis();
        var vision = new Mock<IVisionClient>();
        vision.Setup(v => v.ExtractJson(It.IsAny<VisionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientAnalysisException("OpenRouter retornou 429"));
        var service = Service(vision.Object, Extractor(TextExtractionEngine.NativeText, ""));

        for (var attempt = 0; attempt < DocumentAnalysis.MaxAttempts + 2; attempt++)
        {
            await service.Analyze(7);
        }

        Assert.Equal(DocumentAnalysisStatus.Pending, analysis.Status);
        Assert.NotNull(analysis.NextAttemptAt);
    }

    [Fact]
    public async Task Should_Fill_Missing_Expiration_Date_From_Text_Reading()
    {
        var analysis = PendingAnalysis();

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(new DateOnly(2026, 8, 9), analysis.Document.ExpirationDate);
        var finding = Assert.Single(analysis.Findings, f => f.Code == "EXPIRATION_DATE_FILLED");
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.DoesNotContain(analysis.Findings, f => f.Code == "EXPIRATION_DATE_DETECTED");
    }

    [Fact]
    public async Task Should_Correct_Wrong_Expiration_Date_From_Text_Reading_And_Warn()
    {
        var analysis = PendingAnalysis(expirationDate: new DateOnly(2026, 12, 31));

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(new DateOnly(2026, 8, 9), analysis.Document.ExpirationDate);
        var finding = Assert.Single(analysis.Findings, f => f.Code == "EXPIRATION_DATE_CORRECTED");
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("31/12/2026", finding.Message);
        Assert.DoesNotContain(analysis.Findings, f => f.Code == "EXPIRATION_DATE_MISMATCH");
    }

    [Fact]
    public async Task Should_Only_Suggest_Expiration_Date_Read_By_Vision()
    {
        var analysis = PendingAnalysis(expirationDate: new DateOnly(2026, 12, 31));

        await Service(Vision(AnalysisTestData.CrfJson()).Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(7);

        Assert.Equal(new DateOnly(2026, 12, 31), analysis.Document.ExpirationDate);
        Assert.Contains(analysis.Findings, f => f.Code == "EXPIRATION_DATE_MISMATCH");
    }

    [Fact]
    public async Task Should_Not_Change_Expiration_Date_Of_Document_Already_Reviewed()
    {
        var analysis = PendingAnalysis();
        analysis.Document.Approve();

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Null(analysis.Document.ExpirationDate);
        Assert.Equal(DocumentAnalysisStatus.Completed, analysis.Status);
    }

    [Fact]
    public async Task Should_Notify_Supplier_When_File_Looks_Like_Another_Document()
    {
        var analysis = PendingAnalysis();

        await Service(Vision(AnalysisTestData.CrfJson(isExpectedDocument: false, detectedDocument: "CNH")).Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(7);

        _notifications.Verify(n => n.DocumentLooksWrong(analysis.Document), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Notify_Supplier_When_Document_Is_The_Expected_One()
    {
        PendingAnalysis();

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        _notifications.Verify(n => n.DocumentLooksWrong(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Notify_Supplier_When_Document_Was_Already_Reviewed()
    {
        var analysis = PendingAnalysis();
        analysis.Document.Reject("Documento errado");

        await Service(Vision(AnalysisTestData.CrfJson(isExpectedDocument: false, detectedDocument: "CNH")).Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(7);

        _notifications.Verify(n => n.DocumentLooksWrong(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Should_Compare_Ai_Verdict_With_Reviewer_Decision()
    {
        _documents.Setup(d => d.GetReviewed(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync([
            Reviewed("FGTS_CND", DocumentStatus.Approved, AnalysisVerdict.Conforming),
            Reviewed("FGTS_CND", DocumentStatus.Rejected, AnalysisVerdict.NonConforming),
            Reviewed("FGTS_CND", DocumentStatus.Approved, AnalysisVerdict.NonConforming),
            Reviewed("FGTS_CND", DocumentStatus.Rejected, AnalysisVerdict.Conforming),
            Reviewed("FGTS_CND", DocumentStatus.Approved, AnalysisVerdict.NeedsAttention),
            Reviewed("PAYMENT_RECEIPT", DocumentStatus.Rejected, AnalysisVerdict.NeedsAttention),
            Reviewed("PAYMENT_RECEIPT", DocumentStatus.Approved, null, DocumentAnalysisStatus.NotSupported),
            Reviewed("PAYMENT_RECEIPT", DocumentStatus.Approved, null, null)
        ]);

        var metrics = await Service().GetMetrics(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        Assert.Equal(8, metrics.Total.Reviewed);
        Assert.Equal(2, metrics.Total.Agreements);
        Assert.Equal(1, metrics.Total.FalseAlarms);
        Assert.Equal(1, metrics.Total.MissedProblems);
        Assert.Equal(1, metrics.Total.AttentionApproved);
        Assert.Equal(1, metrics.Total.AttentionRejected);
        Assert.Equal(2, metrics.Total.NotAnalyzed);
        Assert.Equal(0.5m, metrics.Total.AgreementRate);

        var crf = Assert.Single(metrics.DocumentTypes, t => t.DocumentTypeCode == "FGTS_CND");
        Assert.Equal(5, crf.Reviewed);
        var receipt = Assert.Single(metrics.DocumentTypes, t => t.DocumentTypeCode == "PAYMENT_RECEIPT");
        Assert.Null(receipt.AgreementRate);
        _documents.Verify(d => d.GetReviewed(
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)), Times.Once);
    }

    private static ReviewedDocument Reviewed(
        string code,
        DocumentStatus status,
        AnalysisVerdict? verdict,
        DocumentAnalysisStatus? analysisStatus = DocumentAnalysisStatus.Completed)
    {
        return new ReviewedDocument(code == "FGTS_CND" ? 4 : 20, code, code, status, analysisStatus, verdict);
    }

    [Fact]
    public async Task Should_Schedule_Period_Cross_Check_When_Recurring_Document_Is_Analyzed()
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.ReceiptDocument()) { Id = 8 };
        _analyses.Setup(a => a.GetById(8)).ReturnsAsync(analysis);

        await Service(Vision(AnalysisTestData.ReceiptJson()).Object, Extractor(TextExtractionEngine.NativeText, "")).Analyze(8);

        _periodCompliance.Verify(p => p.RequestRecalculation(3, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Schedule_Period_Cross_Check_For_Onboarding_Document()
    {
        PendingAnalysis();

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        _periodCompliance.Verify(p => p.RequestRecalculation(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task Should_Warn_When_Vision_Reading_Is_Not_Confirmed_By_Ocr()
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.ReceiptDocument()) { Id = 8 };
        _analyses.Setup(a => a.GetById(8)).ReturnsAsync(analysis);
        var ocr = "Recibo de Pagamento CONSTRUTORA EXEMPLO LTDA 11.222.333/0001-81 Julho de 2026 MARIA APARECIDA DOS SANTOS CPF 529.982.247-25 Líquido a Receber 1.500,00";

        await Service(
            Vision(AnalysisTestData.ReceiptJson(employeeSigned: "true")).Object,
            Extractor(TextExtractionEngine.NativeText, ""),
            Extractor(TextExtractionEngine.Ocr, ocr)).Analyze(8);

        Assert.Equal(TextExtractionEngine.Vision, analysis.Engine);
        var finding = Assert.Single(analysis.Findings);
        Assert.Equal("VISION_NOT_CONFIRMED_BY_OCR", finding.Code);
        Assert.Contains("1900.75", finding.Message);
    }

    [Fact]
    public async Task Should_Read_With_Ocr_Without_Calling_Vision_When_Ocr_Is_Enough()
    {
        var analysis = PendingAnalysis();
        var vision = Vision(AnalysisTestData.CrfJson());

        await Service(vision.Object, Extractor(TextExtractionEngine.NativeText, ""), Extractor(TextExtractionEngine.Ocr, AnalysisTestData.CrfText())).Analyze(7);

        Assert.Equal(TextExtractionEngine.Ocr, analysis.Engine);
        vision.Verify(v => v.ExtractJson(It.IsAny<VisionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Ignore_Analysis_That_Is_No_Longer_Pending()
    {
        var analysis = PendingAnalysis();
        analysis.MarkNotSupported();

        await Service(Extractor(TextExtractionEngine.NativeText, AnalysisTestData.CrfText())).Analyze(7);

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Create_Analysis_When_Reanalyzing_Document_Uploaded_Before_The_Feature()
    {
        var document = AnalysisTestData.CrfDocument();
        _documents.Setup(d => d.GetById(document.Id)).ReturnsAsync(document);
        DocumentAnalysis? added = null;
        _analyses.Setup(a => a.Add(It.IsAny<DocumentAnalysis>())).Callback<DocumentAnalysis>(a => added = a);

        var result = await Service().Reanalyze(document.Id);

        Assert.Same(document, added!.Document);
        Assert.Equal(DocumentAnalysisStatus.Pending, result.Status);
    }

    [Fact]
    public async Task Should_Put_Finished_Analysis_Back_In_The_Queue_When_Reanalyzing()
    {
        var analysis = PendingAnalysis();
        analysis.MarkNotSupported();
        _analyses.Setup(a => a.GetByDocumentId(analysis.Document.Id)).ReturnsAsync(analysis);

        var result = await Service().Reanalyze(analysis.Document.Id);

        Assert.Equal(DocumentAnalysisStatus.Pending, result.Status);
        _analyses.Verify(a => a.Add(It.IsAny<DocumentAnalysis>()), Times.Never);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
