using AutoMapper;
using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Services;

public class WorkerService : IWorkerService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodComplianceService _periodComplianceService;

    public WorkerService(IMapper mapper, IUnitOfWork unitOfWork, IPeriodComplianceService periodComplianceService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _periodComplianceService = periodComplianceService;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<PageList<WorkerDto>> Get(PageParams pageParams, WorkerFilter filter)
    {
        var workers = await _unitOfWork.WorkerRepository.Get(pageParams, filter);
        var dtos = _mapper.Map<List<WorkerDto>>(workers.Items);
        return new PageList<WorkerDto>(dtos, workers.TotalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<WorkerDto> GetById(long id, long? companyId = null)
    {
        var worker = await GetExisting(id, companyId);
        return _mapper.Map<WorkerDto>(worker);
    }

    public async Task<WorkerDto> Create(long companyId, WorkerCreateDto model)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        var worker = new Worker(companyId, model.Name, model.Cpf);

        JotanunesException.When(
            await _unitOfWork.WorkerRepository.CpfInUse(companyId, worker.Cpf),
            "Já existe um trabalhador cadastrado com este CPF.");

        _unitOfWork.WorkerRepository.Add(worker);
        await _unitOfWork.SaveChangesAsync();

        return await GetById(worker.Id);
    }

    public async Task<WorkerDto> Update(long id, long companyId, WorkerUpdateDto model)
    {
        var worker = await GetExisting(id, companyId);

        worker.Update(model.Name);

        _unitOfWork.WorkerRepository.Update(worker);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkerDto>(worker);
    }

    public async Task<WorkerDto> Activate(long id, long companyId)
    {
        var worker = await GetExisting(id, companyId);

        worker.Activate();

        _unitOfWork.WorkerRepository.Update(worker);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkerDto>(worker);
    }

    public async Task<WorkerDto> Deactivate(long id, long companyId)
    {
        var worker = await GetExisting(id, companyId);

        worker.Deactivate();
        _unitOfWork.WorkerRepository.Update(worker);

        var allocations = await _unitOfWork.WorkerAllocationRepository.GetActiveByWorker(worker.Id);
        foreach (var allocation in allocations.Where(a => !a.SupplyRequest.IsClosed))
        {
            allocation.Release();
            _unitOfWork.WorkerAllocationRepository.Update(allocation);
            await _periodComplianceService.RequestRecalculationFrom(allocation.SupplyRequestId, Today);
        }

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkerDto>(worker);
    }

    public async Task<List<WorkerAllocationDto>> GetAllocations(long supplyRequestId, long? companyId = null, bool includeReleased = false)
    {
        await GetExistingSupplyRequest(supplyRequestId, companyId);

        var allocations = await _unitOfWork.WorkerAllocationRepository.GetBySupplyRequest(supplyRequestId, activeOnly: !includeReleased);
        return _mapper.Map<List<WorkerAllocationDto>>(allocations);
    }

    public async Task<WorkerAllocationDto> Allocate(long supplyRequestId, long companyId, WorkerAllocateDto model)
    {
        var supplyRequest = await GetExistingSupplyRequest(supplyRequestId, companyId);
        var worker = await GetExisting(model.WorkerId, companyId);

        JotanunesException.When(
            await _unitOfWork.WorkerAllocationRepository.GetActive(supplyRequestId, worker.Id) is not null,
            "Trabalhador já está alocado nesta solicitação.");

        var activeCount = await _unitOfWork.WorkerAllocationRepository.CountActive(supplyRequestId);
        var allocation = new WorkerAllocation(supplyRequest, worker, activeCount);

        _unitOfWork.WorkerAllocationRepository.Add(allocation);
        await _periodComplianceService.RequestRecalculationFrom(supplyRequestId, Today);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkerAllocationDto>(allocation);
    }

    public async Task<WorkerAllocationDto> Release(long supplyRequestId, long workerId, long? companyId = null)
    {
        var supplyRequest = await GetExistingSupplyRequest(supplyRequestId, companyId);
        supplyRequest.EnsureNotClosed();

        var allocation = await _unitOfWork.WorkerAllocationRepository.GetActive(supplyRequestId, workerId);
        if (allocation is null)
        {
            throw new KeyNotFoundException("Trabalhador não está alocado nesta solicitação");
        }

        allocation.Release();

        _unitOfWork.WorkerAllocationRepository.Update(allocation);
        await _periodComplianceService.RequestRecalculationFrom(supplyRequestId, Today);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<WorkerAllocationDto>(allocation);
    }

    private async Task<Worker> GetExisting(long id, long? companyId = null)
    {
        var worker = await _unitOfWork.WorkerRepository.GetById(id);
        if (worker is null || (companyId.HasValue && worker.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Trabalhador não encontrado");
        }

        return worker;
    }

    private async Task<SupplyRequest> GetExistingSupplyRequest(long id, long? companyId = null)
    {
        var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(id);
        if (supplyRequest is null || (companyId.HasValue && supplyRequest.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }

        return supplyRequest;
    }
}
