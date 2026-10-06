using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(
    IDocumentService documentService,
    IDocumentComplianceService complianceService,
    IDocumentAnalysisService analysisService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<InternalDocumentDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] InternalDocumentFilter filter)
    {
        var result = await documentService.GetForReview(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InternalDocumentDto>> GetById(long id)
    {
        var document = await documentService.GetByIdForReview(id);
        return Ok(document);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id)
    {
        var download = await documentService.Download(id);
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<InternalDocumentDto>> Approve(long id)
    {
        var document = await documentService.Approve(id);
        return Ok(document);
    }

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<InternalDocumentDto>> Reject(long id, [FromBody] DocumentRejectDto rejectDto)
    {
        var document = await documentService.Reject(id, rejectDto.Reason);
        return Ok(document);
    }

    [HttpGet("{id:long}/analysis")]
    public async Task<ActionResult<DocumentAnalysisDto>> GetAnalysis(long id)
    {
        var analysis = await analysisService.GetByDocument(id);
        return Ok(analysis);
    }

    [HttpPost("{id:long}/analysis")]
    public async Task<ActionResult<DocumentAnalysisDto>> Reanalyze(long id)
    {
        var analysis = await analysisService.Reanalyze(id);
        return Accepted(analysis);
    }

    [HttpGet("checklist/{supplyRequestId:long}")]
    public async Task<ActionResult<ComplianceChecklistDto>> GetChecklist(long supplyRequestId)
    {
        var checklist = await complianceService.GetChecklist(supplyRequestId);
        return Ok(checklist);
    }
}
