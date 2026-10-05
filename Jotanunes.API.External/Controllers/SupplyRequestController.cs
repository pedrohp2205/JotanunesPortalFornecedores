using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.SupplyRequests;
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
public class SupplyRequestController(ISupplyRequestService supplyRequestService, IWorkerService workerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<SupplyRequestDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] SupplyRequestFilter filter)
    {
        filter.CompanyId = User.GetCompanyId();
        var result = await supplyRequestService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SupplyRequestDto>> GetById(long id)
    {
        var supplyRequest = await supplyRequestService.GetById(id, User.GetCompanyId());
        return Ok(supplyRequest);
    }

    [HttpGet("{id:long}/workers")]
    public async Task<ActionResult<List<WorkerAllocationDto>>> GetWorkers(long id, [FromQuery] bool includeReleased = false)
    {
        var allocations = await workerService.GetAllocations(id, User.GetCompanyId(), includeReleased);
        return Ok(allocations);
    }

    [HttpPost("{id:long}/workers")]
    public async Task<ActionResult<WorkerAllocationDto>> AllocateWorker(long id, [FromBody] WorkerAllocateDto allocateDto)
    {
        var allocation = await workerService.Allocate(id, User.GetCompanyId(), allocateDto);
        return Ok(allocation);
    }

    [HttpDelete("{id:long}/workers/{workerId:long}")]
    public async Task<ActionResult<WorkerAllocationDto>> ReleaseWorker(long id, long workerId)
    {
        var allocation = await workerService.Release(id, workerId, User.GetCompanyId());
        return Ok(allocation);
    }
}
