using AutoMapper;
using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
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
