using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Interfaces;

public interface ICompanyService
{
    Task<PageList<CompanyDto>> Get(PageParams pageParams, CompanyFilter filter);
    Task<CompanyDto> GetById(long id);
    Task<CompanyDto> Create(CompanyCreateDto model);
    Task<CompanyDto> Update(long id, CompanyUpdateDto model);
    Task<CompanyDto> Delete(long id);
}
