using Jotanunes.Domain.Entities;

namespace Jotanunes.Domain.Interfaces;

public interface IDocumentAnalysisRepository : IGenericRepository<DocumentAnalysis>
{
    Task<DocumentAnalysis?> GetById(long id);
    Task<DocumentAnalysis?> GetByDocumentId(long documentId);
    Task<List<long>> GetPendingIds(int take);
}
