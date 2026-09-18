namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository CompanyRepository { get; }
    ISupplierUserRepository SupplierUserRepository { get; }
    IWorkSiteRepository WorkSiteRepository { get; }
    ISupplyRequestRepository SupplyRequestRepository { get; }
    IDocumentTypeRepository DocumentTypeRepository { get; }
    IDocumentRepository DocumentRepository { get; }
    Task<bool> SaveChangesAsync();
}
