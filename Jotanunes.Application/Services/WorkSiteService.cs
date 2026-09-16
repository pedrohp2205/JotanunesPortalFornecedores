using AutoMapper;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;

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

    public async Task<List<WorkSiteDto>> GetByCompany(long companyId)
    {
        var workSites = await _unitOfWork.WorkSiteRepository.GetByCompany(companyId);
        return _mapper.Map<List<WorkSiteDto>>(workSites);
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

    public async Task<List<CompanyDto>> GetCompanies(long workSiteId)
    {
        await GetExistingWorkSite(workSiteId);

        var companies = await _unitOfWork.WorkSiteRepository.GetCompanies(workSiteId);
        return _mapper.Map<List<CompanyDto>>(companies);
    }

    public async Task LinkCompany(long workSiteId, long companyId)
    {
        await GetExistingWorkSite(workSiteId);

        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        var existingLink = await _unitOfWork.WorkSiteRepository.GetLink(workSiteId, companyId);
        JotanunesException.When(existingLink is not null, "Empresa já está vinculada a esta obra.");

        _unitOfWork.WorkSiteRepository.AddLink(new CompanyWorkSite(companyId, workSiteId));
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UnlinkCompany(long workSiteId, long companyId)
    {
        var link = await _unitOfWork.WorkSiteRepository.GetLink(workSiteId, companyId);
        if (link is null)
        {
            throw new KeyNotFoundException("Vínculo entre empresa e obra não encontrado");
        }

        _unitOfWork.WorkSiteRepository.RemoveLink(link);
        await _unitOfWork.SaveChangesAsync();
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
}
