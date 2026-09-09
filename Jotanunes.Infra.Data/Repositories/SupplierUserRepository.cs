using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class SupplierUserRepository : GenericRepository<SupplierUser>, ISupplierUserRepository
{
    private readonly ApplicationDbContext _context;

    public SupplierUserRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<SupplierUser?> GetById(long id)
    {
        return await _context.SupplierUsers
                                .Include(u => u.Company)
                                .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<SupplierUser?> GetByEmail(string email)
    {
        var normalized = email.Trim().ToLower();
        return await _context.SupplierUsers
                                .Include(u => u.Company)
                                .FirstOrDefaultAsync(u => u.Email == normalized);
    }

    public async Task<SupplierUser?> GetByRefreshToken(string refreshToken)
    {
        return await _context.SupplierUsers
                                .Include(u => u.Company)
                                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task<List<SupplierUser>> GetByCompany(long companyId)
    {
        return await _context.SupplierUsers
                                .AsNoTracking()
                                .Where(u => u.CompanyId == companyId)
                                .OrderBy(u => u.Name)
                                .ToListAsync();
    }

    public async Task<bool> EmailInUse(string email, long? ignoreUserId = null)
    {
        var normalized = email.Trim().ToLower();
        return await _context.SupplierUsers
                                .AnyAsync(u => u.Email == normalized && (!ignoreUserId.HasValue || u.Id != ignoreUserId.Value));
    }
}
