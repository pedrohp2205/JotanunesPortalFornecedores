namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository CompanyRepository { get; }
    ISupplierUserRepository SupplierUserRepository { get; }
    IWorkSiteRepository WorkSiteRepository { get; }
    Task<bool> SaveChangesAsync();
}
