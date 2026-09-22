using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentTypeController(IDocumentTypeService documentTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplierDocumentTypeDto>>> Get([FromQuery] SupplierType? supplierType)
    {
        var result = await documentTypeService.GetForCompany(User.GetCompanyId(), supplierType);
        return Ok(result);
    }
}
