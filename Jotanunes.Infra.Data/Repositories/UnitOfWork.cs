using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;

namespace Jotanunes.Infra.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private ICompanyRepository? _companyRepository;
    private ISupplierUserRepository? _supplierUserRepository;
    private IWorkSiteRepository? _workSiteRepository;
    private IDocumentTypeRepository? _documentTypeRepository;
    private IDocumentRepository? _documentRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ICompanyRepository CompanyRepository
    {
        get
        {
            return _companyRepository ??= new CompanyRepository(_context);
        }
    }

    public ISupplierUserRepository SupplierUserRepository
    {
        get
        {
            return _supplierUserRepository ??= new SupplierUserRepository(_context);
        }
    }

    public IWorkSiteRepository WorkSiteRepository
    {
        get
        {
            return _workSiteRepository ??= new WorkSiteRepository(_context);
        }
    }

    public IDocumentTypeRepository DocumentTypeRepository
    {
        get
        {
            return _documentTypeRepository ??= new DocumentTypeRepository(_context);
        }
    }

    public IDocumentRepository DocumentRepository
    {
        get
        {
            return _documentRepository ??= new DocumentRepository(_context);
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
