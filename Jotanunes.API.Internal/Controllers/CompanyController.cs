using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController(ICompanyService companyService, ISupplierUserService supplierUserService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageList<CompanyDto>>> Get([FromQuery] PageParams pageParams, [FromQuery] CompanyFilter filter)
    {
        var result = await companyService.Get(pageParams, filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CompanyDto>> GetById(long id)
    {
        var company = await companyService.GetById(id);
        return Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CompanyCreateDto companyDto)
    {
        var createdCompany = await companyService.Create(companyDto);
        return Ok(createdCompany);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CompanyDto>> Update(long id, [FromBody] CompanyUpdateDto companyDto)
    {
        var updatedCompany = await companyService.Update(id, companyDto);
        return Ok(updatedCompany);
    }

    [HttpPut("{id:long}/supplier-type")]
    public async Task<ActionResult<CompanyDto>> ChangeSupplierType(long id, [FromBody] CompanyChangeSupplierTypeDto supplierTypeDto)
    {
        var company = await companyService.ChangeSupplierType(id, supplierTypeDto);
        return Ok(company);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CompanyDto>> Delete(long id)
    {
        var deletedCompany = await companyService.Delete(id);
        return Ok(deletedCompany);
    }

    [HttpGet("{id:long}/users")]
    public async Task<ActionResult<List<SupplierUserDto>>> GetUsers(long id)
    {
        var users = await supplierUserService.GetByCompany(id);
        return Ok(users);
    }

    [HttpPost("{id:long}/users")]
    public async Task<ActionResult<SupplierUserDto>> AddUser(long id, [FromBody] SupplierUserCreateDto userDto)
    {
        var createdUser = await supplierUserService.Create(id, userDto);
        return Ok(createdUser);
    }

    [HttpPost("{id:long}/users/{userId:long}/activate")]
    public async Task<ActionResult<SupplierUserDto>> ActivateUser(long id, long userId)
    {
        var user = await supplierUserService.Activate(id, userId);
        return Ok(user);
    }

    [HttpPost("{id:long}/users/{userId:long}/deactivate")]
    public async Task<ActionResult<SupplierUserDto>> DeactivateUser(long id, long userId)
    {
        var user = await supplierUserService.Deactivate(id, userId);
        return Ok(user);
    }

    [HttpPost("{id:long}/users/{userId:long}/reset-password")]
    public async Task<ActionResult<SupplierUserDto>> ResetUserPassword(long id, long userId, [FromBody] SupplierUserResetPasswordDto resetDto)
    {
        var user = await supplierUserService.ResetPassword(id, userId, resetDto);
        return Ok(user);
    }
}
