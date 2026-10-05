namespace Jotanunes.Application.DTOs.Compliance;

public class WorkerComplianceDto
{
    public long WorkerId { get; set; }
    public string WorkerCpf { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public DateTime AllocatedAt { get; set; }
    public bool IsUpToDate { get; set; }

    public List<ChecklistItemDto> OnboardingItems { get; set; } = [];

    public List<ChecklistItemDto> Items { get; set; } = [];
}
