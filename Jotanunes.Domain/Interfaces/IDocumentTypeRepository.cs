using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Interfaces;

public interface IDocumentTypeRepository : IGenericRepository<DocumentType>
{
    Task<List<DocumentType>> Get();
    Task<DocumentType?> GetById(long id);
    Task<bool> CodeInUse(string code, long? ignoreDocumentTypeId = null);
    Task<List<DocumentType>> GetApplicable(SupplierType supplierType);
}
