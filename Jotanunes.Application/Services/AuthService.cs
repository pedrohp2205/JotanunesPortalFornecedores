using AutoMapper;
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

    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
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
        return await IssueTokens(user);
    }

    public async Task<TokenDto> Refresh(RefreshTokenDto model)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetByRefreshToken(model.RefreshToken);
        if (user is null || !user.IsRefreshTokenValid(model.RefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        if (!user.Active)
        {
            throw new UnauthorizedAccessException("Usuário inativo. Procure o responsável da Jotanunes.");
        }

        return await IssueTokens(user);
    }

    public async Task Logout(long userId)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        user.RevokeRefreshToken();
        _unitOfWork.SupplierUserRepository.Update(user);
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

    public async Task ChangePassword(long userId, ChangePasswordDto model)
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
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<TokenDto> IssueTokens(SupplierUser user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.AssignRefreshToken(refreshToken.Token, refreshToken.ExpiresAt);

        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new TokenDto
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            User = _mapper.Map<AuthenticatedUserDto>(user)
        };
    }
}
