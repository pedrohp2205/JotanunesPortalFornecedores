using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class SupplierUserSession : BaseEntity
{
    public const int ReuseGraceSeconds = 30;

    public long SupplierUserId { get; private set; }
    public SupplierUser SupplierUser { get; private set; } = null!;
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public string? PreviousRefreshTokenHash { get; private set; }
    public DateTime? RotatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string SecurityStamp { get; private set; } = string.Empty;

    protected SupplierUserSession() { }

    public SupplierUserSession(SupplierUser user, string refreshTokenHash, DateTime expiresAt)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(refreshTokenHash), "Refresh token não pode ser vazio.");

        SupplierUserId = user.Id;
        SupplierUser = user;
        SecurityStamp = user.SecurityStamp;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
    }

    public bool IsActive()
    {
        return RevokedAt is null
            && ExpiresAt > DateTime.UtcNow
            && SupplierUser.Active
            && SecurityStamp == SupplierUser.SecurityStamp;
    }

    public bool IsPreviousToken(string refreshTokenHash)
    {
        return PreviousRefreshTokenHash is not null && PreviousRefreshTokenHash == refreshTokenHash;
    }

    public bool IsWithinReuseGrace()
    {
        return RotatedAt.HasValue && RotatedAt.Value.AddSeconds(ReuseGraceSeconds) > DateTime.UtcNow;
    }

    public void Rotate(string newRefreshTokenHash, DateTime expiresAt)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(newRefreshTokenHash), "Refresh token não pode ser vazio.");

        PreviousRefreshTokenHash = RefreshTokenHash;
        RefreshTokenHash = newRefreshTokenHash;
        RotatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void Revoke()
    {
        RevokedAt ??= DateTime.UtcNow;
    }
}
