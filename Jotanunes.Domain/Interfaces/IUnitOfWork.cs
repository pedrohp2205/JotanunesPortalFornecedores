namespace Jotanunes.Domain.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository CompanyRepository { get; }
    ISupplierUserRepository SupplierUserRepository { get; }
    ISupplierUserSessionRepository SupplierUserSessionRepository { get; }
    IWorkSiteRepository WorkSiteRepository { get; }
    ISupplyRequestRepository SupplyRequestRepository { get; }
    IDocumentTypeRepository DocumentTypeRepository { get; }
    IDocumentRepository DocumentRepository { get; }
    IWorkerRepository WorkerRepository { get; }
    IWorkerAllocationRepository WorkerAllocationRepository { get; }
    IDocumentAnalysisRepository DocumentAnalysisRepository { get; }
    Task<bool> SaveChangesAsync();
}
