using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;
using Jotanunes.Domain.Projections;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PageList<Document>> Get(PageParams pageParams, DocumentFilter filter)
    {
        IQueryable<Document> query = ApplyFilter(
            _context.Documents
                .AsNoTracking()
                .Include(d => d.Company)
                .Include(d => d.DocumentType)
                .Include(d => d.SupplyRequest!).ThenInclude(sr => sr.WorkSite)
                .Include(d => d.Worker)
                .Include(d => d.UploadedBySupplierUser)
                .Include(d => d.Analysis),
            filter);

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(d => d.CreatedAt)
                            .Skip((pageParams.PageNumber - 1) * pageParams.PageSize)
                            .Take(pageParams.PageSize)
                            .ToListAsync();

        return new PageList<Document>(items, totalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<List<Document>> GetAll(DocumentFilter filter)
    {
        IQueryable<Document> query = ApplyFilter(_context.Documents.AsNoTracking(), filter);

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<List<Document>> GetBySupplyRequests(IReadOnlyCollection<long> supplyRequestIds, DateOnly? referencePeriodFrom = null)
    {
        var query = _context.Documents
                                .AsNoTracking()
                                .Where(d => d.SupplyRequestId != null && supplyRequestIds.Contains(d.SupplyRequestId.Value));

        if (referencePeriodFrom.HasValue)
        {
            query = query.Where(d => d.ReferencePeriodEnd == null || d.ReferencePeriodEnd >= referencePeriodFrom.Value);
        }

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<List<Document>> GetOnboardingByCompanies(IReadOnlyCollection<long> companyIds)
    {
        return await _context.Documents
                                .AsNoTracking()
                                .Where(d => d.SupplyRequestId == null && d.WorkerId == null && companyIds.Contains(d.CompanyId))
                                .OrderByDescending(d => d.CreatedAt)
                                .ToListAsync();
    }

    public async Task<List<Document>> GetOnboardingByWorkers(IReadOnlyCollection<long> workerIds)
    {
        return await _context.Documents
                                .AsNoTracking()
                                .Where(d => d.SupplyRequestId == null && d.WorkerId != null && workerIds.Contains(d.WorkerId.Value))
                                .OrderByDescending(d => d.CreatedAt)
                                .ToListAsync();
    }

    public async Task<Document?> GetById(long id)
    {
        return await _context.Documents
                                .Include(d => d.Company)
                                .Include(d => d.DocumentType)
                                .Include(d => d.SupplyRequest!).ThenInclude(sr => sr.WorkSite)
                                .Include(d => d.Worker)
                                .Include(d => d.UploadedBySupplierUser)
                                .Include(d => d.Analysis)
                                .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<ReviewedDocument>> GetReviewed(DateTime? reviewedFrom, DateTime? reviewedTo)
    {
        var query = _context.Documents
                                .AsNoTracking()
                                .Where(d => d.Status != DocumentStatus.Pending && d.ReviewedAt != null);

        if (reviewedFrom.HasValue)
        {
            query = query.Where(d => d.ReviewedAt >= reviewedFrom.Value);
        }

        if (reviewedTo.HasValue)
        {
            query = query.Where(d => d.ReviewedAt < reviewedTo.Value);
        }

        return await query
                        .Select(d => new ReviewedDocument(
                            d.DocumentTypeId,
                            d.DocumentType.Code,
                            d.DocumentType.Name,
                            d.Status,
                            d.Analysis != null ? d.Analysis.Status : null,
                            d.Analysis != null ? d.Analysis.Verdict : null))
                        .ToListAsync();
    }

    private static IQueryable<Document> ApplyFilter(IQueryable<Document> query, DocumentFilter filter)
    {
        if (filter.CompanyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == filter.CompanyId.Value);
        }

        if (filter.WorkSiteId.HasValue)
        {
            query = query.Where(d => d.SupplyRequest != null && d.SupplyRequest.WorkSiteId == filter.WorkSiteId.Value);
        }

        if (filter.SupplyRequestId.HasValue)
        {
            query = query.Where(d => d.SupplyRequestId == filter.SupplyRequestId.Value);
        }

        if (filter.WithoutSupplyRequest == true)
        {
            query = query.Where(d => d.SupplyRequestId == null);
        }

        if (filter.DocumentTypeId.HasValue)
        {
            query = query.Where(d => d.DocumentTypeId == filter.DocumentTypeId.Value);
        }

        if (filter.WorkerId.HasValue)
        {
            query = query.Where(d => d.WorkerId == filter.WorkerId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(d => d.Status == filter.Status.Value);
        }

        if (filter.PeriodStart.HasValue)
        {
            query = query.Where(d => d.ReferencePeriodEnd == null || d.ReferencePeriodEnd >= filter.PeriodStart.Value);
        }

        if (filter.PeriodEnd.HasValue)
        {
            query = query.Where(d => d.ReferencePeriodStart == null || d.ReferencePeriodStart <= filter.PeriodEnd.Value);
        }

        if (filter is InternalDocumentFilter { AnalysisStatus: { } analysisStatus })
        {
            query = query.Where(d => d.Analysis != null && d.Analysis.Status == analysisStatus);
        }

        if (filter is InternalDocumentFilter { AnalysisVerdict: { } analysisVerdict })
        {
            query = query.Where(d => d.Analysis != null && d.Analysis.Verdict == analysisVerdict);
        }

        return query;
    }
}
