using AutoMapper;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
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

    private DocumentAnalysisService Service(params IDocumentTextExtractor[] extractors)
    {
        return new DocumentAnalysisService(
            _mapper,
            _unitOfWork.Object,
            _storage.Object,
            extractors,
            [new CrfAnalyzer()],
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<DocumentAnalysisService>.Instance);
    }

    private static IDocumentTextExtractor Extractor(TextExtractionEngine engine, string text)
    {
        var extractor = new Mock<IDocumentTextExtractor>();
        extractor.SetupGet(e => e.Engine).Returns(engine);
        extractor.Setup(e => e.Extract(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentText([text]));
        return extractor.Object;
    }

    private DocumentAnalysis PendingAnalysis(string typeCode = "FGTS_CND")
    {
        var analysis = new DocumentAnalysis(AnalysisTestData.CrfDocument(typeCode: typeCode)) { Id = 7 };
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
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
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
