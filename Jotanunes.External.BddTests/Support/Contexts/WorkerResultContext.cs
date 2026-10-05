using Jotanunes.Application.DTOs.Workers;
using Jotanunes.External.BddTests.Support.Models;

namespace Jotanunes.External.BddTests.Support.Contexts;

public class WorkerResultContext
{
    public WorkerDto? Trabalhador { get; set; }
    public PageListResponseDto<WorkerDto>? Trabalhadores { get; set; }
    public WorkerAllocationDto? Alocacao { get; set; }
    public List<WorkerAllocationDto>? Alocacoes { get; set; }
}
