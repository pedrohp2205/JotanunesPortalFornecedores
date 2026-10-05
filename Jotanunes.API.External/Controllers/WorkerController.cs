using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkerController(IWorkerService workerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<WorkerDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] WorkerFilter filter)
    {
        filter.CompanyId = User.GetCompanyId();
        var result = await workerService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<WorkerDto>> GetById(long id)
    {
        var worker = await workerService.GetById(id, User.GetCompanyId());
        return Ok(worker);
    }

    [HttpPost]
    public async Task<ActionResult<WorkerDto>> Create([FromBody] WorkerCreateDto workerDto)
    {
        var created = await workerService.Create(User.GetCompanyId(), workerDto);
        return Ok(created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<WorkerDto>> Update(long id, [FromBody] WorkerUpdateDto workerDto)
    {
        var updated = await workerService.Update(id, User.GetCompanyId(), workerDto);
        return Ok(updated);
    }

    [HttpPost("{id:long}/activate")]
    public async Task<ActionResult<WorkerDto>> Activate(long id)
    {
        var worker = await workerService.Activate(id, User.GetCompanyId());
        return Ok(worker);
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<ActionResult<WorkerDto>> Deactivate(long id)
    {
        var worker = await workerService.Deactivate(id, User.GetCompanyId());
        return Ok(worker);
    }
}
