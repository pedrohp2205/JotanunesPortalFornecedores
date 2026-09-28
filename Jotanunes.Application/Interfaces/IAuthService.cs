using Jotanunes.Application.DTOs.Auth;

namespace Jotanunes.Application.Interfaces;

public interface IAuthService
{
    Task<TokenDto> Login(LoginDto model);
    Task<TokenDto> Refresh(RefreshTokenDto model);
    Task Logout(long userId, long sessionId);
    Task<AuthenticatedUserDto> GetAuthenticatedUser(long userId);
    Task<TokenDto> ChangePassword(long userId, ChangePasswordDto model);
    Task ForgotPassword(ForgotPasswordDto model);
    Task ResetPassword(ResetPasswordDto model);
}
