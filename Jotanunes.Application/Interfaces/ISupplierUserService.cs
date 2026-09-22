using Jotanunes.Application.DTOs.Users;

namespace Jotanunes.Application.Interfaces;

public interface ISupplierUserService
{
    Task<List<SupplierUserDto>> GetByCompany(long companyId);
    Task<SupplierUserDto> Create(long companyId, SupplierUserCreateDto model);
    Task<SupplierUserDto> Activate(long companyId, long userId);
    Task<SupplierUserDto> Deactivate(long companyId, long userId);
    Task<SupplierUserDto> ResetPassword(long companyId, long userId, SupplierUserResetPasswordDto model);
}
