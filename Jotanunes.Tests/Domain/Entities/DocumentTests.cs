using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests.Domain.Entities;

public class DocumentTests
{
    private static Document CompanyDocument()
    {
        return new Document(
            companyId: 1,
            documentTypeId: 1,
            uploadedBySupplierUserId: 1,
            category: DocumentCategory.Onboarding,
            subject: DocumentSubject.Company,
            storageKey: "companies/1/documents/abc-cnpj.pdf",
            originalFileName: "cnpj.pdf",
            contentType: "application/pdf");
    }

    private static Document WorkerDocument()
    {
        return new Document(
            companyId: 1,
            documentTypeId: 18,
            uploadedBySupplierUserId: 1,
            category: DocumentCategory.Recurring,
            subject: DocumentSubject.Worker,
            storageKey: "companies/1/documents/abc-ponto.pdf",
            originalFileName: "ponto.pdf",
            contentType: "application/pdf",
            supplyRequestId: 10,
            workerId: 7,
            referencePeriodStart: new DateOnly(2026, 7, 1),
            referencePeriodEnd: new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void Should_Create_Company_Document_Pending_Without_Worker()
    {
        var document = CompanyDocument();

        Assert.Equal(DocumentStatus.Pending, document.Status);
        Assert.Null(document.WorkerId);
        Assert.Null(document.SupplyRequestId);
    }

    [Fact]
    public void Should_Create_Worker_Document_Linked_To_Worker()
    {
        var document = WorkerDocument();

        Assert.Equal(7, document.WorkerId);
        Assert.Equal(10, document.SupplyRequestId);
    }

    [Fact]
    public void Should_Throw_Exception_When_Worker_Document_Has_No_Worker_Data()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 18, 1, DocumentCategory.Recurring, DocumentSubject.Worker,
            "companies/1/documents/abc.pdf", "ponto.pdf", "application/pdf",
            supplyRequestId: 10,
            referencePeriodStart: new DateOnly(2026, 7, 1),
            referencePeriodEnd: new DateOnly(2026, 7, 31)));

        Assert.Equal("Trabalhador é obrigatório para este tipo de documento.", ex.Message);
    }

    [Fact]
    public void Should_Create_Worker_Onboarding_Document_Without_SupplyRequest()
    {
        var document = new Document(
            1, 19, 1, DocumentCategory.Onboarding, DocumentSubject.Worker,
            "companies/1/documents/abc-aso.pdf", "aso.pdf", "application/pdf",
            workerId: 7,
            expirationDate: new DateOnly(2027, 1, 31));

        Assert.Equal(7, document.WorkerId);
        Assert.Null(document.SupplyRequestId);
    }

    [Fact]
    public void Should_Throw_Exception_When_Company_Document_Has_Worker_Data()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 1, 1, DocumentCategory.Onboarding, DocumentSubject.Company,
            "companies/1/documents/abc.pdf", "cnpj.pdf", "application/pdf",
            workerId: 7));

        Assert.Equal("Documento de empresa não deve ter trabalhador vinculado.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Recurring_Document_Has_No_SupplyRequest()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 11, 1, DocumentCategory.Recurring, DocumentSubject.Company,
            "companies/1/documents/abc-folha.pdf", "folha.pdf", "application/pdf"));

        Assert.Equal("Documento recorrente precisa estar vinculado a uma solicitação.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Recurring_Document_Has_No_Period()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 11, 1, DocumentCategory.Recurring, DocumentSubject.Company,
            "companies/1/documents/abc-folha.pdf", "folha.pdf", "application/pdf",
            supplyRequestId: 10));

        Assert.Equal("Documento recorrente precisa informar o período de referência.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Recurring_Document_Period_Is_Invalid()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 11, 1, DocumentCategory.Recurring, DocumentSubject.Company,
            "companies/1/documents/abc-folha.pdf", "folha.pdf", "application/pdf",
            supplyRequestId: 10,
            referencePeriodStart: new DateOnly(2026, 7, 31),
            referencePeriodEnd: new DateOnly(2026, 7, 1)));

        Assert.Equal("Período de referência inválido.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Onboarding_Document_Has_Period()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 1, 1, DocumentCategory.Onboarding, DocumentSubject.Company,
            "companies/1/documents/abc-cnpj.pdf", "cnpj.pdf", "application/pdf",
            referencePeriodStart: new DateOnly(2026, 7, 1),
            referencePeriodEnd: new DateOnly(2026, 7, 31)));

        Assert.Equal("Documento de habilitação não deve ter período de referência.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Onboarding_Document_Has_SupplyRequest()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Document(
            1, 1, 1, DocumentCategory.Onboarding, DocumentSubject.Company,
            "companies/1/documents/abc-cnpj.pdf", "cnpj.pdf", "application/pdf",
            supplyRequestId: 10));

        Assert.Equal("Documento de habilitação não deve estar vinculado a uma obra específica.", ex.Message);
    }

    [Fact]
    public void Should_Approve_Pending_Document()
    {
        var document = CompanyDocument();

        document.Approve();

        Assert.Equal(DocumentStatus.Approved, document.Status);
        Assert.NotNull(document.ReviewedAt);
    }

    [Fact]
    public void Should_Reject_Pending_Document_With_Reason()
    {
        var document = CompanyDocument();

        document.Reject("Documento ilegível");

        Assert.Equal(DocumentStatus.Rejected, document.Status);
        Assert.Equal("Documento ilegível", document.RejectionReason);
    }

    [Fact]
    public void Should_Throw_Exception_When_Approving_Already_Reviewed_Document()
    {
        var document = CompanyDocument();
        document.Approve();

        var ex = Assert.Throws<JotanunesException>(() => document.Approve());
        Assert.Equal("Documento já foi avaliado.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Rejecting_Without_Reason()
    {
        var document = CompanyDocument();

        var ex = Assert.Throws<JotanunesException>(() => document.Reject(""));
        Assert.Equal("Motivo da rejeição é obrigatório.", ex.Message);
    }

    [Fact]
    public void Should_Detect_Expired_Document()
    {
        var document = new Document(
            1, 4, 1, DocumentCategory.Onboarding, DocumentSubject.Company,
            "companies/1/documents/abc.pdf", "crf.pdf", "application/pdf",
            expirationDate: new DateOnly(2026, 1, 1));

        Assert.True(document.IsExpired(new DateOnly(2026, 2, 1)));
        Assert.False(document.IsExpired(new DateOnly(2025, 12, 1)));
    }

    [Fact]
    public void Should_Apply_Expiration_Date_Read_By_Analysis_While_Pending()
    {
        var document = CompanyDocument();

        document.ApplyAnalyzedExpirationDate(new DateOnly(2026, 8, 9));

        Assert.Equal(new DateOnly(2026, 8, 9), document.ExpirationDate);
    }

    [Fact]
    public void Should_Not_Apply_Expiration_Date_After_Review()
    {
        var document = CompanyDocument();
        document.Approve();

        Assert.Throws<JotanunesException>(() => document.ApplyAnalyzedExpirationDate(new DateOnly(2026, 8, 9)));
    }
}
