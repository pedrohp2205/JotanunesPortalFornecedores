namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository CompanyRepository { get; }
    ISupplierUserRepository SupplierUserRepository { get; }
    Task<bool> SaveChangesAsync();
}
