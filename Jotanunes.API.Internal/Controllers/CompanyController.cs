using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ISupplierUserService _supplierUserService;

    public CompanyController(ICompanyService companyService, ISupplierUserService supplierUserService)
    {
        _companyService = companyService;
        _supplierUserService = supplierUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PageList<CompanyDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] CompanyFilter filter)
    {
        var result = await _companyService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CompanyDto>> GetById(long id)
    {
        var company = await _companyService.GetById(id);
        return Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CompanyCreateDto companyDto)
    {
        var createdCompany = await _companyService.Create(companyDto);
        return Ok(createdCompany);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CompanyDto>> Update(long id, [FromBody] CompanyUpdateDto companyDto)
    {
        var updatedCompany = await _companyService.Update(id, companyDto);
        return Ok(updatedCompany);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CompanyDto>> Delete(long id)
    {
        var deletedCompany = await _companyService.Delete(id);
        return Ok(deletedCompany);
    }

    [HttpGet("{id:long}/users")]
    public async Task<ActionResult<List<SupplierUserDto>>> GetUsers(long id)
    {
        var users = await _supplierUserService.GetByCompany(id);
        return Ok(users);
    }

    [HttpPost("{id:long}/users")]
    public async Task<ActionResult<SupplierUserDto>> AddUser(long id, [FromBody] SupplierUserCreateDto userDto)
    {
        var createdUser = await _supplierUserService.Create(id, userDto);
        return Ok(createdUser);
    }
}
