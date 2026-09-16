using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IDocumentTypeRepository : IGenericRepository<DocumentType>
{
    Task<List<DocumentType>> Get();
    Task<DocumentType?> GetById(long id);
    Task<bool> CodeInUse(string code, long? ignoreDocumentTypeId = null);
}
