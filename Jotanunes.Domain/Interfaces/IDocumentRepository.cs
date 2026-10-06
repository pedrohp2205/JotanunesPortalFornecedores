using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Jotanunes.Domain.Projections;

namespace Jotanunes.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<PageList<Document>> Get(PageParams pageParams, DocumentFilter filter);
    Task<List<Document>> GetAll(DocumentFilter filter);
    Task<List<Document>> GetBySupplyRequests(IReadOnlyCollection<long> supplyRequestIds, DateOnly? referencePeriodFrom = null);
    Task<List<Document>> GetOnboardingByCompanies(IReadOnlyCollection<long> companyIds);
    Task<List<Document>> GetOnboardingByWorkers(IReadOnlyCollection<long> workerIds);
    Task<Document?> GetById(long id);
    Task<List<Document>> GetForPeriod(long supplyRequestId, DateOnly periodStart, DateOnly periodEnd);
    Task<List<ReviewedDocument>> GetReviewed(DateTime? reviewedFrom, DateTime? reviewedTo);
}
