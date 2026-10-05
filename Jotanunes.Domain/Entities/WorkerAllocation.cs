using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class WorkerAllocation : BaseEntity
{
    public long SupplyRequestId { get; private set; }
    public SupplyRequest SupplyRequest { get; private set; } = null!;
    public long WorkerId { get; private set; }
    public Worker Worker { get; private set; } = null!;
    public DateTime AllocatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    protected WorkerAllocation() { }

    public WorkerAllocation(SupplyRequest supplyRequest, Worker worker, int activeAllocationCount)
    {
        supplyRequest.EnsureCanAllocateWorker(activeAllocationCount);

        JotanunesException.When(worker.CompanyId != supplyRequest.CompanyId, "Trabalhador não pertence à empresa da solicitação.");
        JotanunesException.When(!worker.Active, "Trabalhador inativo não pode ser alocado.");

        SupplyRequestId = supplyRequest.Id;
        SupplyRequest = supplyRequest;
        WorkerId = worker.Id;
        Worker = worker;
        AllocatedAt = DateTime.UtcNow;
    }

    public bool IsActive => ReleasedAt is null;

    public void Release()
    {
        JotanunesException.When(!IsActive, "Trabalhador já foi desalocado desta solicitação.");

        ReleasedAt = DateTime.UtcNow;
    }
}
