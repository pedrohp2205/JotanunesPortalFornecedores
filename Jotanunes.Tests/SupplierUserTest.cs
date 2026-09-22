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

    [Fact]
    public void Should_Require_Password_Change_And_Clear_Lock_When_Password_Is_Reset_By_Admin()
    {
        var user = ValidUser();
        user.SetPassword("hash-atual");
        user.AssignRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));
        for (var i = 0; i < SupplierUser.MaxLoginAttempts; i++)
        {
            user.RegisterFailedLogin();
        }

        user.ResetPassword("hash-provisorio");

        Assert.Equal("hash-provisorio", user.PasswordHash);
        Assert.True(user.MustChangePassword);
        Assert.False(user.IsLocked());
        Assert.Null(user.RefreshToken);
    }

    [Fact]
    public void Should_Accept_Password_Reset_Token_Only_While_Valid_And_Matching()
    {
        var user = ValidUser();
        user.AssignPasswordResetToken("hash-do-token");

        Assert.True(user.IsPasswordResetTokenValid("hash-do-token"));
        Assert.False(user.IsPasswordResetTokenValid("outro-hash"));

        user.ClearPasswordResetToken();
        Assert.False(user.IsPasswordResetTokenValid("hash-do-token"));
    }

    [Fact]
    public void Should_Invalidate_Password_Reset_Token_When_Password_Changes()
    {
        var user = ValidUser();
        user.AssignPasswordResetToken("hash-do-token");

        user.SetPassword("novo-hash");

        Assert.Null(user.PasswordResetTokenHash);
        Assert.False(user.IsPasswordResetTokenValid("hash-do-token"));
    }

    [Fact]
    public void Should_Invalidate_Password_Reset_Token_When_Deactivated()
    {
        var user = ValidUser();
        user.AssignPasswordResetToken("hash-do-token");

        user.Deactivate();

        Assert.Null(user.PasswordResetTokenHash);
    }

    [Fact]
    public void Should_Throttle_Password_Reset_Requests_During_Cooldown()
    {
        var user = ValidUser();
        Assert.True(user.CanRequestPasswordReset());

        user.AssignPasswordResetToken("hash-do-token");

        Assert.False(user.CanRequestPasswordReset());
    }

    [Fact]
    public void Should_Allow_New_Password_Reset_Request_After_Cooldown()
    {
        var user = ValidUser();
        user.AssignPasswordResetToken("hash-do-token");

        typeof(SupplierUser).GetProperty(nameof(SupplierUser.PasswordResetExpiresAt))!
            .SetValue(user, DateTime.UtcNow.AddMinutes(SupplierUser.PasswordResetTokenMinutes - SupplierUser.PasswordResetCooldownMinutes - 1));

        Assert.True(user.CanRequestPasswordReset());
    }
}
