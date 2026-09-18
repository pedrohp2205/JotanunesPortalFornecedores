using AutoMapper;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services;

public class SupplyRequestService : ISupplyRequestService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentComplianceService _complianceService;

    public SupplyRequestService(IMapper mapper, IUnitOfWork unitOfWork, IDocumentComplianceService complianceService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _complianceService = complianceService;
    }

    public async Task<List<SupplyRequestDto>> Get(SupplyRequestFilter filter)
    {
        var supplyRequests = await _unitOfWork.SupplyRequestRepository.Get(filter);
        return _mapper.Map<List<SupplyRequestDto>>(supplyRequests);
    }

    public async Task<SupplyRequestDto> GetById(long id, long? companyId = null)
    {
        var supplyRequest = await GetExisting(id, companyId);
        return _mapper.Map<SupplyRequestDto>(supplyRequest);
    }

    public async Task<SupplyRequestDto> Create(SupplyRequestCreateDto model)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(model.CompanyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        var workSite = await _unitOfWork.WorkSiteRepository.GetById(model.WorkSiteId);
        if (workSite is null)
        {
            throw new KeyNotFoundException("Obra não encontrada");
        }

        JotanunesException.When(
            !company.Supplies(model.SupplierType),
            "A empresa não está cadastrada para fornecer este tipo de serviço.");

        JotanunesException.When(
            await _unitOfWork.SupplyRequestRepository.ActiveExists(model.CompanyId, model.WorkSiteId, model.SupplierType),
            "Já existe uma solicitação ativa desta empresa para esta obra e este tipo de fornecimento.");

        var supplyRequest = new SupplyRequest(model.CompanyId, model.WorkSiteId, model.SupplierType, model.RequiredWorkerCount);

        _unitOfWork.SupplyRequestRepository.Add(supplyRequest);
        await _unitOfWork.SaveChangesAsync();

        return await GetById(supplyRequest.Id);
    }

    public async Task<SupplyRequestDto> Update(long id, SupplyRequestUpdateDto model)
    {
        var supplyRequest = await GetExisting(id);

        supplyRequest.UpdateRequiredWorkerCount(model.RequiredWorkerCount);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SupplyRequestDto>(supplyRequest);
    }

    public async Task<SupplyRequestDto> Complete(long id)
    {
        var supplyRequest = await GetExisting(id);

        supplyRequest.EnsureNotClosed();

        // Concluir significa que o fornecedor entregou tudo: com pendências, só cancelando.
        var pending = await _complianceService.GetPending(id);
        if (pending is not null)
        {
            throw new JotanunesException(BuildPendingMessage(pending));
        }

        supplyRequest.Complete();

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SupplyRequestDto>(supplyRequest);
    }

    public async Task<SupplyRequestDto> Cancel(long id)
    {
        var supplyRequest = await GetExisting(id);

        supplyRequest.Cancel();

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SupplyRequestDto>(supplyRequest);
    }

    private static string BuildPendingMessage(OverdueSupplyRequestDto pending)
    {
        var parts = new List<string>();

        if (pending.MissingOnboardingCount > 0)
        {
            parts.Add($"{pending.MissingOnboardingCount} {Plural(pending.MissingOnboardingCount, "documento de habilitação", "documentos de habilitação")}");
        }

        if (pending.MissingRecurringCompanyCount > 0)
        {
            parts.Add($"{pending.MissingRecurringCompanyCount} {Plural(pending.MissingRecurringCompanyCount, "documento recorrente da empresa", "documentos recorrentes da empresa")}");
        }

        if (pending.RequiredWorkerCount.HasValue && pending.WorkersUpToDate < pending.RequiredWorkerCount.Value)
        {
            var missingWorkers = pending.RequiredWorkerCount.Value - pending.WorkersUpToDate;
            parts.Add($"{missingWorkers} {Plural(missingWorkers, "trabalhador em dia", "trabalhadores em dia")} (meta {pending.RequiredWorkerCount.Value}, em dia {pending.WorkersUpToDate})");
        }

        var missing = parts.Count == 1
            ? parts[0]
            : $"{string.Join(", ", parts.Take(parts.Count - 1))} e {parts[^1]}";

        return $"Não é possível concluir a solicitação: faltam {missing} no período {pending.PeriodStart:dd/MM/yyyy} a {pending.PeriodEnd:dd/MM/yyyy}. Para encerrar com pendências, cancele a solicitação.";
    }

    private static string Plural(int count, string singular, string plural)
    {
        return count == 1 ? singular : plural;
    }

    private async Task<SupplyRequest> GetExisting(long id, long? companyId = null)
    {
        var supplyRequest = await _unitOfWork.SupplyRequestRepository.GetById(id);
        if (supplyRequest is null || (companyId.HasValue && supplyRequest.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Solicitação não encontrada");
        }

        return supplyRequest;
    }
}
