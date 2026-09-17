namespace Jotanunes.Application.DTOs.Compliance;

public class WorkerComplianceDto
{
    public string WorkerCpf { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public bool IsUpToDate { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = [];
}
