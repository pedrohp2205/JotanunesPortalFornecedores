namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository CompanyRepository { get; }
    Task<bool> SaveChangesAsync();
}
