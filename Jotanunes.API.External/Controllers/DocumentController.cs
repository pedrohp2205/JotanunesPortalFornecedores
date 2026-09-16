using Jotanunes.API.External.Models;
using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<DocumentDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] DocumentFilter filter)
    {
        filter.CompanyId = User.GetCompanyId();
        var result = await documentService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DocumentDto>> GetById(long id)
    {
        var document = await documentService.GetById(id, User.GetCompanyId());
        return Ok(document);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id)
    {
        var download = await documentService.Download(id, User.GetCompanyId());
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] DocumentUploadForm form)
    {
        await using var stream = form.File.OpenReadStream();

        var document = await documentService.Upload(
            User.GetCompanyId(),
            User.GetUserId(),
            new DocumentUploadDto
            {
                DocumentTypeId = form.DocumentTypeId,
                CompanyWorkSiteId = form.CompanyWorkSiteId,
                WorkerName = form.WorkerName,
                WorkerCpf = form.WorkerCpf,
                ReferencePeriodStart = form.ReferencePeriodStart,
                ReferencePeriodEnd = form.ReferencePeriodEnd,
                ExpirationDate = form.ExpirationDate
            },
            stream,
            form.File.FileName,
            form.File.ContentType);

        return Ok(document);
    }
}
