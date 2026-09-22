using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentTypeService
{
    Task<List<DocumentTypeDto>> Get();
    Task<List<SupplierDocumentTypeDto>> GetForCompany(long companyId, SupplierType? supplierType = null);
    Task<DocumentTypeDto> GetById(long id);
    Task<DocumentTypeDto> Create(DocumentTypeCreateDto model);
    Task<DocumentTypeDto> Update(long id, DocumentTypeUpdateDto model);
    Task<DocumentTypeDto> Delete(long id);
}
