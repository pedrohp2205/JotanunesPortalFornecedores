using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupplyRequestController(
    ISupplyRequestService supplyRequestService,
    IWorkerService workerService,
    IPeriodComplianceService periodComplianceService,
    IDocumentComplianceService complianceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<SupplyRequestDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] SupplyRequestFilter filter)
    {
        var result = await supplyRequestService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SupplyRequestDto>> GetById(long id)
    {
        var supplyRequest = await supplyRequestService.GetById(id);
        return Ok(supplyRequest);
    }

    // Monta a tela de seleção antes de abrir a solicitação.
    [HttpGet("document-options")]
    public async Task<ActionResult<DocumentOptionsDto>> GetDocumentOptions([FromQuery] long companyId, [FromQuery] SupplierType supplierType)
    {
        var options = await complianceService.GetDocumentOptions(companyId, supplierType);
        return Ok(options);
    }

    // A mesma tela para uma solicitação já aberta, com a seleção atual marcada.
    [HttpGet("{id:long}/document-options")]
    public async Task<ActionResult<DocumentOptionsDto>> GetDocumentOptionsForRequest(long id)
    {
        var supplyRequest = await supplyRequestService.GetById(id);
        var options = await complianceService.GetDocumentOptions(supplyRequest.CompanyId, supplyRequest.SupplierType, id);
        return Ok(options);
    }

    [HttpPut("{id:long}/documents")]
    public async Task<ActionResult<SupplyRequestDto>> UpdateRequiredDocuments(long id, [FromBody] SupplyRequestDocumentsUpdateDto model)
    {
        var supplyRequest = await supplyRequestService.UpdateRequiredDocuments(id, model);
        return Ok(supplyRequest);
    }

    [HttpGet("{id:long}/workers")]
    public async Task<ActionResult<List<WorkerAllocationDto>>> GetWorkers(long id, [FromQuery] bool includeReleased = false)
    {
        var allocations = await workerService.GetAllocations(id, includeReleased: includeReleased);
        return Ok(allocations);
    }

    [HttpGet("{id:long}/compliance-reports")]
    public async Task<ActionResult<List<PeriodComplianceReportDto>>> GetComplianceReports(long id)
    {
        var reports = await periodComplianceService.GetBySupplyRequest(id);
        return Ok(reports);
    }

    [HttpPost("{id:long}/compliance-reports")]
    public async Task<ActionResult<PeriodComplianceReportDto>> RecalculateComplianceReport(long id, [FromBody] PeriodComplianceRecalculateDto model)
    {
        var report = await periodComplianceService.Recalculate(id, model);
        return Accepted(report);
    }

    [HttpDelete("{id:long}/workers/{workerId:long}")]
    public async Task<ActionResult<WorkerAllocationDto>> ReleaseWorker(long id, long workerId)
    {
        var allocation = await workerService.Release(id, workerId);
        return Ok(allocation);
    }

    [HttpPost]
    public async Task<ActionResult<SupplyRequestDto>> Create([FromBody] SupplyRequestCreateDto supplyRequestDto)
    {
        var created = await supplyRequestService.Create(supplyRequestDto);
        return Ok(created);
    }
    
    [HttpPost("with-new-company")]
    public async Task<ActionResult<SupplyRequestDto>> CreateWithNewCompany([FromBody] SupplyRequestWithNewCompanyCreateDto supplyRequestDto)
    {
        var created = await supplyRequestService.CreateWithNewCompany(supplyRequestDto);
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
