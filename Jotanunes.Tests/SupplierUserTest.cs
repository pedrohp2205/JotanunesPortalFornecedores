using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests;

public class SupplierUserTest
{
    private static SupplierUser ValidUser()
    {
        return new SupplierUser(1, "Maria Souza", "Maria@Exemplo.com.br", "hash-da-senha");
    }

    [Fact]
    public void Should_Create_User_Normalizing_Email_And_Requiring_Password_Change()
    {
        var user = ValidUser();

        Assert.Equal("maria@exemplo.com.br", user.Email);
        Assert.True(user.Active);
        Assert.True(user.MustChangePassword);
        Assert.False(user.IsLocked());
    }

    [Fact]
    public void Should_Throw_Exception_When_Email_Is_Invalid()
    {
        var ex = Assert.Throws<JotanunesException>(() => new SupplierUser(1, "Maria Souza", "maria-exemplo", "hash"));
        Assert.Equal("E-mail deve conter '@' e '.'", ex.Message);
    }

    [Fact]
    public void Should_Lock_User_After_Max_Failed_Login_Attempts()
    {
        var user = ValidUser();

        for (var i = 0; i < SupplierUser.MaxLoginAttempts; i++)
        {
            user.RegisterFailedLogin();
        }

        Assert.True(user.IsLocked());
    }

    [Fact]
    public void Should_Not_Lock_User_Before_Max_Failed_Login_Attempts()
    {
        var user = ValidUser();

        for (var i = 0; i < SupplierUser.MaxLoginAttempts - 1; i++)
        {
            user.RegisterFailedLogin();
        }

        Assert.False(user.IsLocked());
    }

    [Fact]
    public void Should_Clear_Lock_And_Attempts_On_Successful_Access()
    {
        var user = ValidUser();
        user.RegisterFailedLogin();

        user.RegisterAccess();

        Assert.NotNull(user.LastAccessAt);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.False(user.IsLocked());
    }

    [Fact]
    public void Should_Validate_Refresh_Token_Only_While_Not_Expired()
    {
        var user = ValidUser();
        user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));

        Assert.True(user.IsRefreshTokenValid("token-abc"));
        Assert.False(user.IsRefreshTokenValid("outro-token"));

        user.RevokeRefreshToken();
        Assert.False(user.IsRefreshTokenValid("token-abc"));
    }

    [Fact]
    public void Should_Invalidate_Refresh_Token_When_Expired()
    {
        var user = ValidUser();
        user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddMinutes(-1));

        Assert.False(user.IsRefreshTokenValid("token-abc"));
    }

    [Fact]
    public void Should_Reset_Session_State_When_Password_Is_Changed()
    {
        var user = ValidUser();
        user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));

        user.SetPassword("novo-hash");

        Assert.Equal("novo-hash", user.PasswordHash);
        Assert.False(user.MustChangePassword);
        Assert.Null(user.RefreshToken);
    }

    [Fact]
    public void Should_Revoke_Refresh_Token_When_Deactivated()
    {
        var user = ValidUser();
        user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));

        user.Deactivate();

        Assert.False(user.Active);
        Assert.Null(user.RefreshToken);
    }
}
