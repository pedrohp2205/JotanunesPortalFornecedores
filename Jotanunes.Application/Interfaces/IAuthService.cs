using Jotanunes.Application.DTOs.Auth;

namespace Jotanunes.Application.Interfaces;

public interface IAuthService
{
    Task<TokenDto> Login(LoginDto model);
    Task<TokenDto> Refresh(RefreshTokenDto model);
    Task Logout(long userId);
    Task<AuthenticatedUserDto> GetAuthenticatedUser(long userId);
    Task ChangePassword(long userId, ChangePasswordDto model);
}
