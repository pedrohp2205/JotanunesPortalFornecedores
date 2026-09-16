using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkSiteController(IWorkSiteService workSiteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CompanyWorkSiteDto>>> Get()
    {
        var result = await workSiteService.GetByCompany(User.GetCompanyId());
        return Ok(result);
    }
}
