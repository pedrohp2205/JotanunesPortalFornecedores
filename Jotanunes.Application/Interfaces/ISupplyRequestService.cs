using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Domain.Filters;

namespace Jotanunes.Application.Interfaces;

public interface ISupplyRequestService
{
    Task<List<SupplyRequestDto>> Get(SupplyRequestFilter filter);
    Task<SupplyRequestDto> GetById(long id, long? companyId = null);
    Task<SupplyRequestDto> Create(SupplyRequestCreateDto model);
    Task<SupplyRequestDto> CreateWithNewCompany(SupplyRequestWithNewCompanyCreateDto model);
    Task<SupplyRequestDto> Update(long id, SupplyRequestUpdateDto model);
    Task<SupplyRequestDto> Complete(long id);
    Task<SupplyRequestDto> Cancel(long id);
}
