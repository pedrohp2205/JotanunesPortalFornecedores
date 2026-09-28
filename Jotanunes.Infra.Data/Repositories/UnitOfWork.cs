using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private static readonly int[] UniqueViolationErrors = [2601, 2627];

    private static readonly Dictionary<string, string> UniqueIndexMessages = new()
    {
        ["IX_supply_requests_CompanyId_WorkSiteId_SupplierType"] = "Já existe uma solicitação ativa desta empresa para esta obra e este tipo de fornecimento.",
        ["IX_companies_Cnpj"] = "Já existe uma empresa cadastrada com este CNPJ.",
        ["IX_supplier_users_Email"] = "Já existe um usuário cadastrado com este e-mail.",
        ["IX_document_types_Code"] = "Já existe um tipo de documento com este código.",
        ["IX_workers_CompanyId_Cpf"] = "Já existe um trabalhador cadastrado com este CPF.",
        ["IX_worker_allocations_SupplyRequestId_WorkerId"] = "Trabalhador já está alocado nesta solicitação."
    };

    private readonly ApplicationDbContext _context;
    private ICompanyRepository? _companyRepository;
    private ISupplierUserRepository? _supplierUserRepository;
    private ISupplierUserSessionRepository? _supplierUserSessionRepository;
    private IWorkSiteRepository? _workSiteRepository;
    private ISupplyRequestRepository? _supplyRequestRepository;
    private IDocumentTypeRepository? _documentTypeRepository;
    private IDocumentRepository? _documentRepository;
    private IWorkerRepository? _workerRepository;
    private IWorkerAllocationRepository? _workerAllocationRepository;
    private IDocumentAnalysisRepository? _documentAnalysisRepository;

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

    public ISupplierUserSessionRepository SupplierUserSessionRepository
    {
        get
        {
            return _supplierUserSessionRepository ??= new SupplierUserSessionRepository(_context);
        }
    }

    public IWorkSiteRepository WorkSiteRepository
    {
        get
        {
            return _workSiteRepository ??= new WorkSiteRepository(_context);
        }
    }

    public ISupplyRequestRepository SupplyRequestRepository
    {
        get
        {
            return _supplyRequestRepository ??= new SupplyRequestRepository(_context);
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

    public IWorkerRepository WorkerRepository
    {
        get
        {
            return _workerRepository ??= new WorkerRepository(_context);
        }
    }

    public IWorkerAllocationRepository WorkerAllocationRepository
    {
        get
        {
            return _workerAllocationRepository ??= new WorkerAllocationRepository(_context);
        }
    }

    public IDocumentAnalysisRepository DocumentAnalysisRepository
    {
        get
        {
            return _documentAnalysisRepository ??= new DocumentAnalysisRepository(_context);
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && UniqueViolationErrors.Contains(sql.Number))
        {
            var message = UniqueIndexMessages
                .Where(m => sql.Message.Contains(m.Key, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Value)
                .FirstOrDefault() ?? "Já existe um registro com estes dados.";

            throw new JotanunesException(message);
        }
    }
}
