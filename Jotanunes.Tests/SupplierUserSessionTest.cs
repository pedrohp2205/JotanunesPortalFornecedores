using Jotanunes.Domain.Entities;

namespace Jotanunes.Tests;

public class SupplierUserSessionTest
{
    private readonly SupplierUser _user = new(1, "Maria Souza", "maria@exemplo.com.br", "hash") { Id = 10 };

    private SupplierUserSession Session(DateTime? expiresAt = null) =>
        new(_user, "hash-1", expiresAt ?? DateTime.UtcNow.AddDays(7));

    [Fact]
    public void Should_Be_Active_While_Not_Expired_Revoked_Or_Invalidated()
    {
        var session = Session();

        Assert.True(session.IsActive());
        Assert.Equal(_user.Id, session.SupplierUserId);
    }

    [Fact]
    public void Should_Be_Inactive_When_Expired()
    {
        Assert.False(Session(DateTime.UtcNow.AddMinutes(-1)).IsActive());
    }

    [Fact]
    public void Should_Be_Inactive_When_Revoked()
    {
        var session = Session();

        session.Revoke();

        Assert.False(session.IsActive());
    }

    [Fact]
    public void Should_Fall_When_User_Changes_Password_Or_Is_Deactivated()
    {
        var beforePasswordChange = Session();
        _user.SetPassword("novo-hash");
        Assert.False(beforePasswordChange.IsActive());

        var afterPasswordChange = Session();
        Assert.True(afterPasswordChange.IsActive());

        _user.Deactivate();
        Assert.False(afterPasswordChange.IsActive());
    }

    [Fact]
    public void Should_Keep_Previous_Hash_On_Rotation()
    {
        var session = Session();

        session.Rotate("hash-2", DateTime.UtcNow.AddDays(7));

        Assert.Equal("hash-2", session.RefreshTokenHash);
        Assert.True(session.IsPreviousToken("hash-1"));
        Assert.False(session.IsPreviousToken("hash-2"));
        Assert.True(session.IsWithinReuseGrace());
    }
}
