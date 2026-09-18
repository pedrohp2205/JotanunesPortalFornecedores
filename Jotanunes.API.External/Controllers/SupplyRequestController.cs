using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplyRequestController(ISupplyRequestService supplyRequestService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplyRequestDto>>> Get([FromQuery] SupplyRequestFilter filter)
    {
        filter.CompanyId = User.GetCompanyId();
        var result = await supplyRequestService.Get(filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SupplyRequestDto>> GetById(long id)
    {
        var supplyRequest = await supplyRequestService.GetById(id, User.GetCompanyId());
        return Ok(supplyRequest);
    }
}
