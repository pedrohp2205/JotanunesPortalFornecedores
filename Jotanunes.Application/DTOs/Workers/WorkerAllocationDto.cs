namespace Jotanunes.Application.DTOs.Workers;

public class WorkerAllocationDto
{
    public long Id { get; set; }
    public long SupplyRequestId { get; set; }
    public long WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerCpf { get; set; } = string.Empty;
    public string WorkerFormattedCpf { get; set; } = string.Empty;
    public bool WorkerActive { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}
