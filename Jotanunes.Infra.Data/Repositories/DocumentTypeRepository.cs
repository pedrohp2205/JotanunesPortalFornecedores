using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class DocumentTypeRepository : GenericRepository<DocumentType>, IDocumentTypeRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentTypeRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<DocumentType>> Get()
    {
        return await _context.DocumentTypes
                                .AsNoTracking()
                                .OrderBy(d => d.Name)
                                .ToListAsync();
    }

    public async Task<DocumentType?> GetById(long id)
    {
        return await _context.DocumentTypes
                                .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> CodeInUse(string code, long? ignoreDocumentTypeId = null)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _context.DocumentTypes
                                .AnyAsync(d => d.Code == normalized && (!ignoreDocumentTypeId.HasValue || d.Id != ignoreDocumentTypeId.Value));
    }

    public async Task<List<DocumentType>> GetApplicable(SupplierType supplierType)
    {
        var both = SupplierType.Material | SupplierType.ManpowerLabor;

        return await _context.DocumentTypes
                                .AsNoTracking()
                                .Where(d => d.Active && (d.AppliesTo == supplierType || d.AppliesTo == both))
                                .OrderBy(d => d.Category)
                                .ThenBy(d => d.Name)
                                .ToListAsync();
    }
}
