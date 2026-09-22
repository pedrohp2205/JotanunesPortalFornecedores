using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class AuthServicePasswordResetTest
{
    private const string Token = "token-de-redefinicao";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplierUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ISupplierNotificationService> _notifications = new();
    private readonly SupplierUser _user = new(1, "Maria Souza", "maria@exemplo.com.br", "hash-atual");
    private readonly AuthService _service;

    public AuthServicePasswordResetTest()
    {
        _unitOfWork.SetupGet(u => u.SupplierUserRepository).Returns(_users.Object);
        _users.Setup(r => r.GetByEmail("maria@exemplo.com.br")).ReturnsAsync(_user);
        _tokenService.Setup(t => t.GeneratePasswordResetToken()).Returns(Token);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-novo");

        _service = new AuthService(
            new Mock<IMapper>().Object,
            _unitOfWork.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _notifications.Object);
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static ResetPasswordDto ResetDto(string token = Token)
    {
        return new ResetPasswordDto
        {
            Email = "maria@exemplo.com.br",
            Token = token,
            NewPassword = "NovaSenha#123",
            NewPasswordConfirmation = "NovaSenha#123"
        };
    }

    [Fact]
    public async Task Should_Store_Only_The_Token_Hash_And_Send_The_Plain_Token()
    {
        await _service.ForgotPassword(new ForgotPasswordDto { Email = "maria@exemplo.com.br" });

        Assert.Equal(Hash(Token), _user.PasswordResetTokenHash);
        _notifications.Verify(n => n.PasswordResetRequested(_user, Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Answer_Silently_When_Email_Is_Unknown()
    {
        _users.Setup(r => r.GetByEmail(It.IsAny<string>())).ReturnsAsync((SupplierUser?)null);

        await _service.ForgotPassword(new ForgotPasswordDto { Email = "ninguem@exemplo.com.br" });

        _notifications.Verify(n => n.PasswordResetRequested(It.IsAny<SupplierUser>(), It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Answer_Silently_When_User_Is_Inactive()
    {
        _user.Deactivate();

        await _service.ForgotPassword(new ForgotPasswordDto { Email = "maria@exemplo.com.br" });

        _notifications.Verify(n => n.PasswordResetRequested(It.IsAny<SupplierUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Send_A_New_Email_During_Cooldown()
    {
        var request = new ForgotPasswordDto { Email = "maria@exemplo.com.br" };

        await _service.ForgotPassword(request);
        await _service.ForgotPassword(request);

        _notifications.Verify(n => n.PasswordResetRequested(It.IsAny<SupplierUser>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Should_Reset_Password_With_A_Valid_Token_And_Consume_It()
    {
        await _service.ForgotPassword(new ForgotPasswordDto { Email = "maria@exemplo.com.br" });
        _user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));

        await _service.ResetPassword(ResetDto());

        Assert.Equal("hash-novo", _user.PasswordHash);
        Assert.False(_user.MustChangePassword);
        Assert.Null(_user.PasswordResetTokenHash);
        Assert.Null(_user.RefreshToken);

        await Assert.ThrowsAsync<JotanunesException>(() => _service.ResetPassword(ResetDto()));
    }

    [Fact]
    public async Task Should_Reject_A_Wrong_Token()
    {
        await _service.ForgotPassword(new ForgotPasswordDto { Email = "maria@exemplo.com.br" });

        await Assert.ThrowsAsync<JotanunesException>(() => _service.ResetPassword(ResetDto("token-errado")));

        Assert.Equal("hash-atual", _user.PasswordHash);
    }

    [Fact]
    public async Task Should_Reject_Reset_When_Email_Is_Unknown()
    {
        _users.Setup(r => r.GetByEmail(It.IsAny<string>())).ReturnsAsync((SupplierUser?)null);

        await Assert.ThrowsAsync<JotanunesException>(() => _service.ResetPassword(ResetDto()));
    }

    [Fact]
    public async Task Should_Reject_Reset_When_Token_Has_Expired()
    {
        await _service.ForgotPassword(new ForgotPasswordDto { Email = "maria@exemplo.com.br" });
        typeof(SupplierUser).GetProperty(nameof(SupplierUser.PasswordResetExpiresAt))!
            .SetValue(_user, DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<JotanunesException>(() => _service.ResetPassword(ResetDto()));
    }
}
