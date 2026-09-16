using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkSiteController(IWorkSiteService workSiteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WorkSiteDto>>> Get()
    {
        var result = await workSiteService.Get();
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<WorkSiteDto>> GetById(long id)
    {
        var workSite = await workSiteService.GetById(id);
        return Ok(workSite);
    }

    [HttpPost]
    public async Task<ActionResult<WorkSiteDto>> Create([FromBody] WorkSiteCreateDto workSiteDto)
    {
        var createdWorkSite = await workSiteService.Create(workSiteDto);
        return Ok(createdWorkSite);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<WorkSiteDto>> Update(long id, [FromBody] WorkSiteUpdateDto workSiteDto)
    {
        var updatedWorkSite = await workSiteService.Update(id, workSiteDto);
        return Ok(updatedWorkSite);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<WorkSiteDto>> Delete(long id)
    {
        var deletedWorkSite = await workSiteService.Delete(id);
        return Ok(deletedWorkSite);
    }

    [HttpGet("{id:long}/companies")]
    public async Task<ActionResult<List<WorkSiteCompanyDto>>> GetCompanies(long id)
    {
        var companies = await workSiteService.GetCompanies(id);
        return Ok(companies);
    }

    [HttpPost("{id:long}/companies/{companyId:long}")]
    public async Task<ActionResult> LinkCompany(long id, long companyId, [FromBody] LinkCompanyToWorkSiteDto? linkDto)
    {
        await workSiteService.LinkCompany(id, companyId, linkDto?.RequiredWorkerCount);
        return NoContent();
    }

    [HttpPut("{id:long}/companies/{companyId:long}")]
    public async Task<ActionResult<WorkSiteCompanyDto>> UpdateRequiredWorkerCount(long id, long companyId, [FromBody] LinkCompanyToWorkSiteDto linkDto)
    {
        var link = await workSiteService.UpdateRequiredWorkerCount(id, companyId, linkDto.RequiredWorkerCount);
        return Ok(link);
    }

    [HttpDelete("{id:long}/companies/{companyId:long}")]
    public async Task<ActionResult> UnlinkCompany(long id, long companyId)
    {
        await workSiteService.UnlinkCompany(id, companyId);
        return NoContent();
    }
}
