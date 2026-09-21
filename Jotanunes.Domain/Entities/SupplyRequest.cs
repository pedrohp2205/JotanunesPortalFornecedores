using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

// Solicitação da Jotanunes a um fornecedor para uma obra. É ela que intermedeia
// as duas partes: define o tipo de fornecimento (material ou mão de obra) e é a
// âncora dos documentos recorrentes. Uma empresa que fornece os dois tipos
// recebe uma solicitação para cada tipo.
public class SupplyRequest : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public long WorkSiteId { get; private set; }
    public WorkSite WorkSite { get; private set; } = null!;
    public SupplierType SupplierType { get; private set; }
    public int? RequiredWorkerCount { get; private set; }
    public SupplyRequestStatus Status { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    protected SupplyRequest() { }

    public SupplyRequest(long companyId, long workSiteId, SupplierType supplierType, int? requiredWorkerCount = null)
    {
        Validate(supplierType, requiredWorkerCount);

        CompanyId = companyId;
        WorkSiteId = workSiteId;
        SupplierType = supplierType;
        RequiredWorkerCount = requiredWorkerCount;
        Status = SupplyRequestStatus.Open;
    }
    
    public SupplyRequest(Company company, long workSiteId, SupplierType supplierType, int? requiredWorkerCount = null)
        : this(company.Id, workSiteId, supplierType, requiredWorkerCount)
    {
        Company = company;
    }

    public bool IsClosed => Status is SupplyRequestStatus.Completed or SupplyRequestStatus.Cancelled;

    public void UpdateRequiredWorkerCount(int? requiredWorkerCount)
    {
        EnsureNotClosed();
        Validate(SupplierType, requiredWorkerCount);

        RequiredWorkerCount = requiredWorkerCount;
    }

    // Chamado no primeiro envio de documento; nas demais chamadas não muda nada.
    public void MarkInProgress()
    {
        EnsureNotClosed();

        if (Status == SupplyRequestStatus.Open)
        {
            Status = SupplyRequestStatus.InProgress;
        }
    }

    public void Complete()
    {
        EnsureNotClosed();

        Status = SupplyRequestStatus.Completed;
        ClosedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        EnsureNotClosed();

        Status = SupplyRequestStatus.Cancelled;
        ClosedAt = DateTime.UtcNow;
    }

    public void EnsureNotClosed()
    {
        JotanunesException.When(IsClosed, "Solicitação encerrada não aceita alterações nem novos documentos.");
    }

    private static void Validate(SupplierType supplierType, int? requiredWorkerCount)
    {
        JotanunesException.When(
            supplierType is not (SupplierType.Material or SupplierType.ManpowerLabor),
            "Tipo de fornecimento da solicitação deve ser Material ou Mão de obra.");

        JotanunesException.When(requiredWorkerCount is < 0, "Quantidade de trabalhadores necessária não pode ser negativa.");

        JotanunesException.When(
            requiredWorkerCount.HasValue && supplierType != SupplierType.ManpowerLabor,
            "Quantidade de trabalhadores necessária só se aplica a solicitações de mão de obra.");
    }
}
