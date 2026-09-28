using AutoMapper;
using System.Security.Cryptography;
using System.Text;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services;

public class AuthService : IAuthService
{
    // Mensagem única para e-mail inexistente e senha errada, para não permitir
    // descobrir quais e-mails estão cadastrados no portal.
    private const string InvalidCredentials = "E-mail ou senha inválidos.";
    private const string InvalidResetToken = "Link de redefinição inválido ou expirado. Solicite um novo.";
    private const string InvalidRefreshToken = "Sessão inválida ou expirada. Faça login novamente.";

    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISupplierNotificationService _notificationService;

    public AuthService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ISupplierNotificationService notificationService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _notificationService = notificationService;
    }

    public async Task<TokenDto> Login(LoginDto model)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetByEmail(model.Email);
        if (user is null)
        {
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        if (user.IsLocked())
        {
            throw new UnauthorizedAccessException(
                "Acesso bloqueado temporariamente por excesso de tentativas. Tente novamente em alguns minutos.");
        }

        if (!user.Active)
        {
            throw new UnauthorizedAccessException("Usuário inativo. Procure o responsável da Jotanunes.");
        }

        if (!_passwordHasher.Verify(model.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin();
            _unitOfWork.SupplierUserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        user.RegisterAccess();
        return await StartSession(user);
    }

    public async Task<TokenDto> Refresh(RefreshTokenDto model)
    {
        var tokenHash = HashToken(model.RefreshToken);

        var session = await _unitOfWork.SupplierUserSessionRepository.GetByRefreshTokenHash(tokenHash);
        if (session is null || session.RevokedAt is not null)
        {
            throw new UnauthorizedAccessException(InvalidRefreshToken);
        }

        if (session.IsPreviousToken(tokenHash))
        {
            if (!session.IsWithinReuseGrace())
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync();
            }

            throw new UnauthorizedAccessException(InvalidRefreshToken);
        }

        if (!session.IsActive())
        {
            throw new UnauthorizedAccessException(InvalidRefreshToken);
        }

        var refreshToken = _tokenService.GenerateRefreshToken();
        session.Rotate(HashToken(refreshToken.Token), refreshToken.ExpiresAt);
        await _unitOfWork.SaveChangesAsync();

        return BuildTokens(session, refreshToken.Token);
    }

    public async Task Logout(long userId, long sessionId)
    {
        var session = await _unitOfWork.SupplierUserSessionRepository.GetById(sessionId);
        if (session is null || session.SupplierUserId != userId)
        {
            throw new KeyNotFoundException("Sessão não encontrada");
        }

        session.Revoke();
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<AuthenticatedUserDto> GetAuthenticatedUser(long userId)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        return _mapper.Map<AuthenticatedUserDto>(user);
    }

    public async Task<TokenDto> ChangePassword(long userId, ChangePasswordDto model)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        JotanunesException.When(
            !_passwordHasher.Verify(model.CurrentPassword, user.PasswordHash),
            "Senha atual incorreta.");

        JotanunesException.When(
            model.CurrentPassword == model.NewPassword,
            "A nova senha deve ser diferente da senha atual.");

        user.SetPassword(_passwordHasher.Hash(model.NewPassword));

        _unitOfWork.SupplierUserRepository.Update(user);
        return await StartSession(user);
    }

    public async Task ForgotPassword(ForgotPasswordDto model)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetByEmail(model.Email);
        if (user is null || !user.Active || !user.CanRequestPasswordReset())
        {
            return;
        }

        var token = _tokenService.GeneratePasswordResetToken();

        user.AssignPasswordResetToken(HashToken(token));
        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.PasswordResetRequested(user, token);
    }

    public async Task ResetPassword(ResetPasswordDto model)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetByEmail(model.Email);
        if (user is null || !user.Active || !user.IsPasswordResetTokenValid(HashToken(model.Token)))
        {
            throw new JotanunesException(InvalidResetToken);
        }

        user.SetPassword(_passwordHasher.Hash(model.NewPassword));

        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private async Task<TokenDto> StartSession(SupplierUser user)
    {
        var refreshToken = _tokenService.GenerateRefreshToken();
        var session = new SupplierUserSession(user, HashToken(refreshToken.Token), refreshToken.ExpiresAt);

        _unitOfWork.SupplierUserSessionRepository.Add(session);
        await _unitOfWork.SaveChangesAsync();

        return BuildTokens(session, refreshToken.Token);
    }

    private TokenDto BuildTokens(SupplierUserSession session, string refreshToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(session.SupplierUser, session.Id);

        return new TokenDto
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken,
            ExpiresAt = accessToken.ExpiresAt,
            User = _mapper.Map<AuthenticatedUserDto>(session.SupplierUser)
        };
    }
}
