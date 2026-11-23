using AutoMapper;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jotanunes.Tests.Application.Mapping;

public class SupplierTypeMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [InlineData(SupplierType.Material, "Material")]
    [InlineData(SupplierType.ManpowerLabor, "ManpowerLabor")]
    [InlineData(SupplierType.Both, "Material, ManpowerLabor")]
    public void Should_Keep_The_Description_Returned_By_The_Api(SupplierType type, string expected)
    {
        var documentType = new DocumentType("ASO", "Atestado", DocumentCategory.Onboarding, type, DocumentSubject.Worker);

        var dto = _mapper.Map<DocumentTypeDto>(documentType);

        Assert.Equal(expected, dto.AppliesToDescription);
    }

    [Fact]
    public void Both_Should_Be_The_Combination_Of_The_Two_Types()
    {
        Assert.Equal(3, (int)SupplierType.Both);
        Assert.Equal(SupplierType.Material | SupplierType.ManpowerLabor, SupplierType.Both);
    }
}
