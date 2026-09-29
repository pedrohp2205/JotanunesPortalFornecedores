using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests.Application.Services;

public class AuthServiceSessionTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplierUserRepository> _users = new();
    private readonly Mock<ISupplierUserSessionRepository> _sessions = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly SupplierUser _user = new(1, "Maria Souza", "maria@exemplo.com.br", "hash-atual") { Id = 10 };
    private readonly AuthService _service;
    private int _issued;

    public AuthServiceSessionTests()
    {
        _unitOfWork.SetupGet(u => u.SupplierUserRepository).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.SupplierUserSessionRepository).Returns(_sessions.Object);
        _users.Setup(r => r.GetByEmail(It.IsAny<string>())).ReturnsAsync(_user);
        _users.Setup(r => r.GetById(_user.Id)).ReturnsAsync(_user);
        _passwordHasher.Setup(h => h.Verify("senha-certa", "hash-atual")).Returns(true);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-novo");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns(() => ($"refresh-{++_issued}", DateTime.UtcNow.AddDays(7)));
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<SupplierUser>(), It.IsAny<long>())).Returns(("access", DateTime.UtcNow.AddMinutes(30)));

        _service = new AuthService(
            new Mock<IMapper>().Object,
            _unitOfWork.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            new Mock<ISupplierNotificationService>().Object);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private SupplierUserSession ExistingSession(string token)
    {
        var session = new SupplierUserSession(_user, Hash(token), DateTime.UtcNow.AddDays(7)) { Id = 99 };
        _sessions.Setup(r => r.GetByRefreshTokenHash(Hash(token))).ReturnsAsync(session);
        return session;
    }

    [Fact]
    public async Task Should_Open_A_Session_Storing_Only_The_Refresh_Token_Hash()
    {
        SupplierUserSession? created = null;
        _sessions.Setup(r => r.Add(It.IsAny<SupplierUserSession>())).Callback<SupplierUserSession>(s => created = s);

        var tokens = await _service.Login(new LoginDto { Email = "maria@exemplo.com.br", Password = "senha-certa" });

        Assert.Equal("refresh-1", tokens.RefreshToken);
        Assert.NotNull(created);
        Assert.Equal(Hash("refresh-1"), created!.RefreshTokenHash);
    }

    [Fact]
    public async Task Should_Rotate_The_Refresh_Token_On_Refresh()
    {
        var session = ExistingSession("refresh-antigo");

        var tokens = await _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" });

        Assert.Equal("refresh-1", tokens.RefreshToken);
        Assert.Equal(Hash("refresh-1"), session.RefreshTokenHash);
        Assert.True(session.IsPreviousToken(Hash("refresh-antigo")));
        _tokenService.Verify(t => t.GenerateAccessToken(_user, 99), Times.Once);
    }

    [Fact]
    public async Task Should_Reject_Previous_Token_Without_Revoking_Inside_The_Grace_Window()
    {
        var session = ExistingSession("refresh-antigo");
        await _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" }));

        Assert.Null(session.RevokedAt);
    }

    [Fact]
    public async Task Should_Revoke_Session_When_A_Rotated_Token_Is_Reused_After_The_Grace_Window()
    {
        var session = ExistingSession("refresh-antigo");
        await _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" });
        typeof(SupplierUserSession).GetProperty(nameof(SupplierUserSession.RotatedAt))!
            .SetValue(session, DateTime.UtcNow.AddSeconds(-SupplierUserSession.ReuseGraceSeconds - 1));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" }));

        Assert.NotNull(session.RevokedAt);
        _sessions.Setup(r => r.GetByRefreshTokenHash(Hash("refresh-1"))).ReturnsAsync(session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-1" }));
    }

    [Fact]
    public async Task Should_Reject_Refresh_After_Password_Change()
    {
        ExistingSession("refresh-antigo");
        _user.SetPassword("outro-hash");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.Refresh(new RefreshTokenDto { RefreshToken = "refresh-antigo" }));
    }

    [Fact]
    public async Task Should_Return_A_New_Session_When_Password_Is_Changed()
    {
        var oldSession = ExistingSession("refresh-antigo");

        var tokens = await _service.ChangePassword(_user.Id, new ChangePasswordDto
        {
            CurrentPassword = "senha-certa",
            NewPassword = "NovaSenha#123",
            NewPasswordConfirmation = "NovaSenha#123"
        });

        Assert.Equal("refresh-1", tokens.RefreshToken);
        Assert.False(_user.MustChangePassword);
        Assert.False(oldSession.IsActive());
        _sessions.Verify(r => r.Add(It.Is<SupplierUserSession>(s => s.IsActive())), Times.Once);
    }

    [Fact]
    public async Task Should_Revoke_Only_The_Current_Session_On_Logout()
    {
        var current = new SupplierUserSession(_user, "h1", DateTime.UtcNow.AddDays(7)) { Id = 1 };
        var otherDevice = new SupplierUserSession(_user, "h2", DateTime.UtcNow.AddDays(7)) { Id = 2 };
        _sessions.Setup(r => r.GetById(1)).ReturnsAsync(current);

        await _service.Logout(_user.Id, 1);

        Assert.False(current.IsActive());
        Assert.True(otherDevice.IsActive());
    }

    [Fact]
    public async Task Should_Not_Logout_Session_Of_Another_User()
    {
        var other = new SupplierUser(1, "João", "joao@exemplo.com.br", "hash") { Id = 20 };
        _sessions.Setup(r => r.GetById(1)).ReturnsAsync(new SupplierUserSession(other, "h1", DateTime.UtcNow.AddDays(7)) { Id = 1 });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Logout(_user.Id, 1));
    }
}
