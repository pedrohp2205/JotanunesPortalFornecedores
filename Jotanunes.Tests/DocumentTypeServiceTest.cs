using AutoMapper;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class DocumentTypeServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDocumentTypeRepository> _types = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly DocumentTypeService _service;

    public DocumentTypeServiceTest()
    {
        _unitOfWork.SetupGet(u => u.DocumentTypeRepository).Returns(_types.Object);
        _unitOfWork.SetupGet(u => u.CompanyRepository).Returns(_companies.Object);
        _types.Setup(t => t.GetApplicable(It.IsAny<SupplierType>())).ReturnsAsync(new List<DocumentType>());
        _mapper.Setup(m => m.Map<List<SupplierDocumentTypeDto>>(It.IsAny<object>())).Returns(new List<SupplierDocumentTypeDto>());
        _service = new DocumentTypeService(_mapper.Object, _unitOfWork.Object);
    }

    private void CompanySupplies(SupplierType type)
    {
        var company = new Company(
            "11.222.333/0001-81",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000", "Sala 2"),
            type);
        _companies.Setup(c => c.GetById(1)).ReturnsAsync(company);
    }

    [Fact]
    public async Task Should_Use_All_Company_Supplier_Types_By_Default()
    {
        CompanySupplies(SupplierType.Material | SupplierType.ManpowerLabor);

        await _service.GetForCompany(1);

        _types.Verify(t => t.GetApplicable(SupplierType.Material | SupplierType.ManpowerLabor), Times.Once);
    }

    [Fact]
    public async Task Should_Narrow_To_The_Requested_Type()
    {
        CompanySupplies(SupplierType.Material | SupplierType.ManpowerLabor);

        await _service.GetForCompany(1, SupplierType.ManpowerLabor);

        _types.Verify(t => t.GetApplicable(SupplierType.ManpowerLabor), Times.Once);
    }

    [Fact]
    public async Task Should_Return_Nothing_For_A_Type_The_Company_Does_Not_Supply()
    {
        CompanySupplies(SupplierType.Material);

        var result = await _service.GetForCompany(1, SupplierType.ManpowerLabor);

        Assert.Empty(result);
        _types.Verify(t => t.GetApplicable(It.IsAny<SupplierType>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Find_Missing_Company()
    {
        _companies.Setup(c => c.GetById(It.IsAny<long>())).ReturnsAsync((Company?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetForCompany(99));
    }
}
