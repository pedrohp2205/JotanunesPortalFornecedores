using AutoMapper;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jotanunes.Tests.Application.Mapping;

public class InternalDocumentMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Should_Summarize_Analysis_By_Severity()
    {
        var document = AnalysisTestData.CrfDocument();
        var analysis = new DocumentAnalysis(document);
        analysis.Complete(TextExtractionEngine.NativeText, [], [
            new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "vencida"),
            new AnalysisFinding("CORPORATE_NAME_MISMATCH", FindingSeverity.Warning, "razão social"),
            new AnalysisFinding("EXPIRATION_DATE_DETECTED", FindingSeverity.Info, "validade")
        ]);
        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(document, analysis);

        var dto = _mapper.Map<InternalDocumentDto>(document);

        Assert.Equal(document.Id, dto.Id);
        Assert.Equal("FGTS_CND", dto.DocumentTypeCode);
        Assert.NotNull(dto.Analysis);
        Assert.Equal("Completed", dto.Analysis.StatusDescription);
        Assert.Equal("NonConforming", dto.Analysis.VerdictDescription);
        Assert.Equal(1, dto.Analysis.BlockingCount);
        Assert.Equal(1, dto.Analysis.WarningCount);
    }

    [Fact]
    public void Should_Leave_Summary_Empty_When_Document_Was_Not_Analyzed()
    {
        var dto = _mapper.Map<InternalDocumentDto>(AnalysisTestData.CrfDocument());

        Assert.Null(dto.Analysis);
    }

    [Fact]
    public void Should_Keep_Supplier_Dto_Without_Analysis()
    {
        Assert.Null(typeof(DocumentDto).GetProperty(nameof(InternalDocumentDto.Analysis)));
    }

    private static Document WithWrongDocumentAnalysis(Document document)
    {
        var analysis = new DocumentAnalysis(document);
        analysis.Complete(TextExtractionEngine.Vision, [], [
            new AnalysisFinding(DocumentAnalysis.WrongDocumentTypeCode, FindingSeverity.Blocking, "O arquivo parece ser CNH.")
        ]);
        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(document, analysis);
        return document;
    }

    [Fact]
    public void Should_Warn_Supplier_When_Pending_Document_Looks_Like_Another_Document()
    {
        var dto = _mapper.Map<DocumentDto>(WithWrongDocumentAnalysis(AnalysisTestData.CrfDocument()));

        Assert.Equal(
            "Não conseguimos identificar este arquivo como \"Certidão Negativa de FGTS\". Confira se enviou o documento certo; se não for, envie o documento correto.",
            dto.UploadWarning);
        Assert.DoesNotContain("CNH", dto.UploadWarning);
    }

    [Fact]
    public void Should_Not_Warn_Supplier_After_The_Document_Was_Reviewed()
    {
        var document = WithWrongDocumentAnalysis(AnalysisTestData.CrfDocument());
        document.Approve();

        Assert.Null(_mapper.Map<DocumentDto>(document).UploadWarning);
    }

    [Fact]
    public void Should_Not_Warn_Supplier_About_Other_Findings()
    {
        var document = AnalysisTestData.CrfDocument();
        var analysis = new DocumentAnalysis(document);
        analysis.Complete(TextExtractionEngine.NativeText, [], [new AnalysisFinding("EXPIRED", FindingSeverity.Blocking, "vencida")]);
        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(document, analysis);

        Assert.Null(_mapper.Map<DocumentDto>(document).UploadWarning);
    }
}
