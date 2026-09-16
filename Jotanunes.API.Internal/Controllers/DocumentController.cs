using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<DocumentDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] DocumentFilter filter)
    {
        var result = await documentService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DocumentDto>> GetById(long id)
    {
        var document = await documentService.GetById(id);
        return Ok(document);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id)
    {
        var download = await documentService.Download(id);
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<DocumentDto>> Approve(long id)
    {
        var document = await documentService.Approve(id);
        return Ok(document);
    }

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<DocumentDto>> Reject(long id, [FromBody] DocumentRejectDto rejectDto)
    {
        var document = await documentService.Reject(id, rejectDto.Reason);
        return Ok(document);
    }
}
