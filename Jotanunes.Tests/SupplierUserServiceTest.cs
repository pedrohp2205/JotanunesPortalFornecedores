using AutoMapper;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class SupplierUserServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplierUserRepository> _users = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ISupplierNotificationService> _notifications = new();
    private readonly SupplierUser _user = new(1, "Maria Souza", "maria@exemplo.com.br", "hash-atual");
    private readonly SupplierUserService _service;

    public SupplierUserServiceTest()
    {
        _unitOfWork.SetupGet(u => u.SupplierUserRepository).Returns(_users.Object);
        _users.Setup(r => r.GetById(It.IsAny<long>())).ReturnsAsync(_user);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-provisorio");
        _service = new SupplierUserService(_mapper.Object, _unitOfWork.Object, _passwordHasher.Object, _notifications.Object);
    }

    [Fact]
    public async Task Should_Deactivate_User_And_Revoke_Session()
    {
        _user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));

        await _service.Deactivate(1, 10);

        Assert.False(_user.Active);
        Assert.Null(_user.RefreshToken);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Reactivate_Deactivated_User()
    {
        _user.Deactivate();

        await _service.Activate(1, 10);

        Assert.True(_user.Active);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Reset_Password_Force_Change_And_Notify_User()
    {
        _user.SetPassword("hash-atual");

        await _service.ResetPassword(1, 10, new SupplierUserResetPasswordDto { TemporaryPassword = "Provisoria#123" });

        Assert.Equal("hash-provisorio", _user.PasswordHash);
        Assert.True(_user.MustChangePassword);
        _notifications.Verify(n => n.TemporaryPasswordIssued(_user, "Provisoria#123"), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Find_User_From_Another_Company()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Deactivate(999, 10));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Activate(999, 10));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ResetPassword(999, 10, new SupplierUserResetPasswordDto { TemporaryPassword = "Provisoria#123" }));

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notifications.Verify(n => n.TemporaryPasswordIssued(It.IsAny<SupplierUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Find_Missing_User()
    {
        _users.Setup(r => r.GetById(It.IsAny<long>())).ReturnsAsync((SupplierUser?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Deactivate(1, 10));
    }
}
