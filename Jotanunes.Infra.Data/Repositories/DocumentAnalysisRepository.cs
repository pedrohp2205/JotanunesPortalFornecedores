using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class DocumentAnalysisRepository : GenericRepository<DocumentAnalysis>, IDocumentAnalysisRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentAnalysisRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<DocumentAnalysis?> GetById(long id)
    {
        return await WithDocument().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<DocumentAnalysis?> GetByDocumentId(long documentId)
    {
        return await WithDocument().FirstOrDefaultAsync(a => a.DocumentId == documentId);
    }

    public async Task<List<long>> GetPendingIds(int take, DateTime now)
    {
        return await _context.DocumentAnalyses
                                .AsNoTracking()
                                .Where(a => a.Status == DocumentAnalysisStatus.Pending && (a.NextAttemptAt == null || a.NextAttemptAt <= now))
                                .OrderBy(a => a.Id)
                                .Select(a => a.Id)
                                .Take(take)
                                .ToListAsync();
    }

    private IQueryable<DocumentAnalysis> WithDocument()
    {
        return _context.DocumentAnalyses
                        .Include(a => a.Document).ThenInclude(d => d.Company)
                        .Include(a => a.Document).ThenInclude(d => d.DocumentType)
                        .Include(a => a.Document).ThenInclude(d => d.Worker);
    }
}
