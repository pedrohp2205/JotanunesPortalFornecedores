using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class SupplierUserSessionRepository : GenericRepository<SupplierUserSession>, ISupplierUserSessionRepository
{
    private readonly ApplicationDbContext _context;

    public SupplierUserSessionRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<SupplierUserSession?> GetById(long id)
    {
        return await _context.SupplierUserSessions
                                .Include(s => s.SupplierUser).ThenInclude(u => u.Company)
                                .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SupplierUserSession?> GetByRefreshTokenHash(string refreshTokenHash)
    {
        return await _context.SupplierUserSessions
                                .Include(s => s.SupplierUser).ThenInclude(u => u.Company)
                                .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash
                                                       || s.PreviousRefreshTokenHash == refreshTokenHash);
    }
}
