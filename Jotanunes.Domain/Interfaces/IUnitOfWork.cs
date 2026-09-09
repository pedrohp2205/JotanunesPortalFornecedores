namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<bool> SaveChangesAsync();
}
