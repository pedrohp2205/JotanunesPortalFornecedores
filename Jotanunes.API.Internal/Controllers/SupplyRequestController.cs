using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupplyRequestController(ISupplyRequestService supplyRequestService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplyRequestDto>>> Get([FromQuery] SupplyRequestFilter filter)
    {
        var result = await supplyRequestService.Get(filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SupplyRequestDto>> GetById(long id)
    {
        var supplyRequest = await supplyRequestService.GetById(id);
        return Ok(supplyRequest);
    }

    [HttpPost]
    public async Task<ActionResult<SupplyRequestDto>> Create([FromBody] SupplyRequestCreateDto supplyRequestDto)
    {
        var created = await supplyRequestService.Create(supplyRequestDto);
        return Ok(created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SupplyRequestDto>> Update(long id, [FromBody] SupplyRequestUpdateDto supplyRequestDto)
    {
        var updated = await supplyRequestService.Update(id, supplyRequestDto);
        return Ok(updated);
    }

    [HttpPost("{id:long}/complete")]
    public async Task<ActionResult<SupplyRequestDto>> Complete(long id)
    {
        var completed = await supplyRequestService.Complete(id);
        return Ok(completed);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<SupplyRequestDto>> Cancel(long id)
    {
        var cancelled = await supplyRequestService.Cancel(id);
        return Ok(cancelled);
    }
}
