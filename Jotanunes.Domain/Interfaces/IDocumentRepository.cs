using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<PageList<Document>> Get(PageParams pageParams, DocumentFilter filter);
    Task<List<Document>> GetAll(DocumentFilter filter);
    Task<List<Document>> GetBySupplyRequests(IReadOnlyCollection<long> supplyRequestIds);
    Task<Document?> GetById(long id);
}
