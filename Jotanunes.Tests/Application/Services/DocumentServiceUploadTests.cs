using AutoMapper;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests.Application.Services;

public class DocumentServiceUploadTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<IDocumentTypeRepository> _types = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly DocumentService _service;

    public DocumentServiceUploadTests()
    {
        var company = new Company(
            "11.222.333/0001-81",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000"),
            SupplierType.Material)
        { Id = 1 };

        _unitOfWork.SetupGet(u => u.DocumentRepository).Returns(_documents.Object);
        _unitOfWork.SetupGet(u => u.DocumentTypeRepository).Returns(_types.Object);
        _unitOfWork.SetupGet(u => u.CompanyRepository).Returns(_companies.Object);
        _companies.Setup(c => c.GetById(1)).ReturnsAsync(company);
        _types.Setup(t => t.GetById(10)).ReturnsAsync(
            new DocumentType("CNPJ", "Cartão CNPJ", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company) { Id = 10 });

        _service = new DocumentService(
            new Mock<IMapper>().Object,
            _unitOfWork.Object,
            _storage.Object,
            new Mock<IDocumentComplianceService>().Object,
            new Mock<ISupplierNotificationService>().Object);
    }

    private static MemoryStream Pdf() => new("%PDF-1.7 conteudo"u8.ToArray());

    [Fact]
    public async Task Should_Store_Detected_Content_Type_Instead_Of_The_Client_One()
    {
        Document? added = null;
        _documents.Setup(d => d.Add(It.IsAny<Document>())).Callback<Document>(d => added = d);
        _documents.Setup(d => d.GetById(It.IsAny<long>())).ReturnsAsync(() => added);

        await _service.Upload(1, 5, new DocumentUploadDto { DocumentTypeId = 10 }, Pdf(), "cartao.pdf");

        Assert.Equal("application/pdf", added!.ContentType);
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Store_File_With_Invalid_Format()
    {
        await Assert.ThrowsAsync<JotanunesException>(() =>
            _service.Upload(1, 5, new DocumentUploadDto { DocumentTypeId = 10 }, new MemoryStream("MZ\x90\0"u8.ToArray()), "cartao.pdf"));

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Delete_Stored_File_When_Database_Save_Fails()
    {
        string? storedKey = null;
        _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, CancellationToken>((key, _, _, _) => storedKey = key);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("banco fora do ar"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.Upload(1, 5, new DocumentUploadDto { DocumentTypeId = 10 }, Pdf(), "cartao.pdf"));

        _storage.Verify(s => s.DeleteAsync(storedKey!, It.IsAny<CancellationToken>()), Times.Once);
    }
}
