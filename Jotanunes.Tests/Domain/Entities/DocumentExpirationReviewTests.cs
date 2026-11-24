using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests.Domain.Entities;

public class DocumentExpirationReviewTests
{
    private static readonly DateOnly Informed = new(2027, 1, 10);
    private static readonly DateOnly Extracted = new(2027, 2, 20);
    private static readonly DateOnly Reviewed = new(2027, 3, 30);

    private static Document AnalyzedDocument(DateOnly? informed = null)
    {
        var document = new Document(
            1, 2, 3,
            DocumentCategory.Onboarding,
            DocumentSubject.Company,
            "chave",
            "certidao.pdf",
            "application/pdf",
            expirationDate: informed);

        var analysis = new DocumentAnalysis(document);
        analysis.Complete(
            TextExtractionEngine.Vision,
            [new ExtractedField(DocumentAnalysis.ExpirationFieldName, "20/02/2027")],
            []);

        typeof(Document).GetProperty(nameof(Document.Analysis))!.SetValue(document, analysis);
        return document;
    }

    [Fact]
    public void Should_Show_The_Date_Read_By_The_Ai_Before_Review()
    {
        Assert.Equal(Extracted, AnalyzedDocument(Informed).ExpirationDateForReview);
    }

    [Fact]
    public void Should_Keep_The_Ai_Date_When_Approved_Without_Changes()
    {
        var document = AnalyzedDocument(Informed);

        document.Approve();

        Assert.Equal(Extracted, document.ExpirationDate);
    }

    [Fact]
    public void Should_Keep_The_Admin_Date_When_Approved_After_Review()
    {
        var document = AnalyzedDocument(Informed);

        document.ReviewExpirationDate(Reviewed);
        document.Approve();

        Assert.Equal(Reviewed, document.ExpirationDate);
        Assert.Equal(Reviewed, document.ExpirationDateForReview);
        Assert.NotNull(document.ExpirationDateReviewedAt);
    }

    [Fact]
    public void Should_Not_Change_The_Date_After_Evaluation()
    {
        var document = AnalyzedDocument(Informed);
        document.Approve();

        Assert.Throws<JotanunesException>(() => document.ReviewExpirationDate(Reviewed));
    }
}
