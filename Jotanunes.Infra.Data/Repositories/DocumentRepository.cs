using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;
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
                .Include(d => d.UploadedBySupplierUser),
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

    public async Task<Document?> GetById(long id)
    {
        return await _context.Documents
                                .Include(d => d.Company)
                                .Include(d => d.DocumentType)
                                .Include(d => d.SupplyRequest!).ThenInclude(sr => sr.WorkSite)
                                .Include(d => d.UploadedBySupplierUser)
                                .FirstOrDefaultAsync(d => d.Id == id);
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

        if (filter.DocumentTypeId.HasValue)
        {
            query = query.Where(d => d.DocumentTypeId == filter.DocumentTypeId.Value);
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

        return query;
    }
}
