using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkerController(IWorkerService workerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<WorkerDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] WorkerFilter filter)
    {
        var result = await workerService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<WorkerDto>> GetById(long id)
    {
        var worker = await workerService.GetById(id);
        return Ok(worker);
    }
}
