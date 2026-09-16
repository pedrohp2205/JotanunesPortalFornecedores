using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentTypeController(IDocumentTypeService documentTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DocumentTypeDto>>> Get()
    {
        var result = await documentTypeService.Get();
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DocumentTypeDto>> GetById(long id)
    {
        var documentType = await documentTypeService.GetById(id);
        return Ok(documentType);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentTypeDto>> Create([FromBody] DocumentTypeCreateDto documentTypeDto)
    {
        var createdDocumentType = await documentTypeService.Create(documentTypeDto);
        return Ok(createdDocumentType);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<DocumentTypeDto>> Update(long id, [FromBody] DocumentTypeUpdateDto documentTypeDto)
    {
        var updatedDocumentType = await documentTypeService.Update(id, documentTypeDto);
        return Ok(updatedDocumentType);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<DocumentTypeDto>> Delete(long id)
    {
        var deactivatedDocumentType = await documentTypeService.Delete(id);
        return Ok(deactivatedDocumentType);
    }
}
