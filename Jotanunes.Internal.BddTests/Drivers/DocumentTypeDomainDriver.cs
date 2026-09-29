using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Internal.BddTests.Drivers;

public class DocumentTypeDomainDriver
{
    public static DocumentType CriarTipoDocumentoValido(string codigo)
    {
        return new DocumentType(codigo, "Tipo de Documento Teste", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company);
    }

    public static DocumentTypeCreateDto CriarDocumentTypeCreateDtoValido(string codigo)
    {
        return new DocumentTypeCreateDto
        {
            Code = codigo,
            Name = "Alvará de Funcionamento",
            Category = DocumentCategory.Onboarding,
            AppliesTo = SupplierType.Material,
            Subject = DocumentSubject.Company,
            RequiresExpirationDate = true
        };
    }

    public static DocumentTypeUpdateDto CriarDocumentTypeUpdateDtoValido(string nome, bool condicional = false, string? descricaoCondicao = null)
    {
        return new DocumentTypeUpdateDto
        {
            Name = nome,
            Category = DocumentCategory.Onboarding,
            AppliesTo = SupplierType.Material | SupplierType.ManpowerLabor,
            Subject = DocumentSubject.Company,
            IsConditional = condicional,
            ConditionDescription = descricaoCondicao
        };
    }
}
