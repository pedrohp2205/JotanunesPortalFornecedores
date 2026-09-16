using AutoMapper;
using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services;

public class WorkSiteService : IWorkSiteService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public WorkSiteService(IMapper mapper, IUnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WorkSiteDto>> Get()
    {
        var workSites = await _unitOfWork.WorkSiteRepository.Get();
        return _mapper.Map<List<WorkSiteDto>>(workSites);
    }

    public async Task<WorkSiteDto> GetById(long id)
    {
        var workSite = await GetExistingWorkSite(id);
        return _mapper.Map<WorkSiteDto>(workSite);
    }

    public async Task<List<CompanyWorkSiteDto>> GetByCompany(long companyId)
    {
        var links = await _unitOfWork.WorkSiteRepository.GetByCompany(companyId);
        return _mapper.Map<List<CompanyWorkSiteDto>>(links);
    }

    public async Task<WorkSiteDto> Create(WorkSiteCreateDto model)
    {
        var workSite = new WorkSite(model.Name, model.RenewalPeriodDays);

        _unitOfWork.WorkSiteRepository.Add(workSite);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkSiteDto>(workSite);
    }

    public async Task<WorkSiteDto> Update(long id, WorkSiteUpdateDto model)
    {
        var workSite = await GetExistingWorkSite(id);

        workSite.Update(model.Name, model.RenewalPeriodDays);

        _unitOfWork.WorkSiteRepository.Update(workSite);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkSiteDto>(workSite);
    }

    public async Task<WorkSiteDto> Delete(long id)
    {
        var workSite = await GetExistingWorkSite(id);

        _unitOfWork.WorkSiteRepository.Delete(workSite);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkSiteDto>(workSite);
    }

    public async Task<List<WorkSiteCompanyDto>> GetCompanies(long workSiteId)
    {
        await GetExistingWorkSite(workSiteId);

        var links = await _unitOfWork.WorkSiteRepository.GetCompanies(workSiteId);
        return _mapper.Map<List<WorkSiteCompanyDto>>(links);
    }

    public async Task LinkCompany(long workSiteId, long companyId, int? requiredWorkerCount = null)
    {
        await GetExistingWorkSite(workSiteId);

        var company = await GetExistingCompany(companyId);
        ValidateRequiredWorkerCount(company, requiredWorkerCount);

        var existingLink = await _unitOfWork.WorkSiteRepository.GetLink(workSiteId, companyId);
        JotanunesException.When(existingLink is not null, "Empresa já está vinculada a esta obra.");

        _unitOfWork.WorkSiteRepository.AddLink(new CompanyWorkSite(companyId, workSiteId, requiredWorkerCount));
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UnlinkCompany(long workSiteId, long companyId)
    {
        var link = await GetExistingLink(workSiteId, companyId);

        _unitOfWork.WorkSiteRepository.RemoveLink(link);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<WorkSiteCompanyDto> UpdateRequiredWorkerCount(long workSiteId, long companyId, int? requiredWorkerCount)
    {
        var link = await GetExistingLink(workSiteId, companyId);
        var company = await GetExistingCompany(companyId);
        ValidateRequiredWorkerCount(company, requiredWorkerCount);

        link.UpdateRequiredWorkerCount(requiredWorkerCount);

        await _unitOfWork.SaveChangesAsync();

        return new WorkSiteCompanyDto
        {
            CompanyId = company.Id,
            CorporateName = company.CorporateName,
            TradeName = company.TradeName,
            FormattedCnpj = Cnpj.Format(company.Cnpj),
            RequiredWorkerCount = link.RequiredWorkerCount
        };
    }

    private async Task<WorkSite> GetExistingWorkSite(long id)
    {
        var workSite = await _unitOfWork.WorkSiteRepository.GetById(id);
        if (workSite is null)
        {
            throw new KeyNotFoundException("Obra não encontrada");
        }

        return workSite;
    }

    private async Task<Company> GetExistingCompany(long companyId)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        return company;
    }

    private async Task<CompanyWorkSite> GetExistingLink(long workSiteId, long companyId)
    {
        var link = await _unitOfWork.WorkSiteRepository.GetLink(workSiteId, companyId);
        if (link is null)
        {
            throw new KeyNotFoundException("Vínculo entre empresa e obra não encontrado");
        }

        return link;
    }

    private static void ValidateRequiredWorkerCount(Company company, int? requiredWorkerCount)
    {
        JotanunesException.When(
            requiredWorkerCount.HasValue && company.SupplierType != SupplierType.ManpowerLabor,
            "Quantidade de trabalhadores necessária só se aplica a fornecedores de mão de obra.");
    }
}
