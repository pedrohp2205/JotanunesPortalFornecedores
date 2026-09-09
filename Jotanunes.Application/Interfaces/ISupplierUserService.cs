using Jotanunes.Application.DTOs.Users;

namespace Jotanunes.Application.Interfaces;

// Mínimo necessário para provisionar o acesso que a frente externa autentica.
public interface ISupplierUserService
{
    Task<List<SupplierUserDto>> GetByCompany(long companyId);
    Task<SupplierUserDto> Create(long companyId, SupplierUserCreateDto model);
}
