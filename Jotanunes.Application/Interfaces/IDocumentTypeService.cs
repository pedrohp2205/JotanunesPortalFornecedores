using Jotanunes.Application.DTOs.DocumentTypes;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentTypeService
{
    Task<List<DocumentTypeDto>> Get();
    Task<DocumentTypeDto> GetById(long id);
    Task<DocumentTypeDto> Create(DocumentTypeCreateDto model);
    Task<DocumentTypeDto> Update(long id, DocumentTypeUpdateDto model);
    Task<DocumentTypeDto> Delete(long id);
}
