using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

// Usuário de acesso ao Portal do Fornecedor (frente externa).
// Sempre vinculado a uma empresa, o que garante o isolamento exigido pelo RNF01.
public class SupplierUser : BaseEntity
{
    public const int MaxLoginAttempts = 5;
    public const int LockoutMinutes = 15;

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTime? LastAccessAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    protected SupplierUser() { }

    public SupplierUser(long companyId, string name, string email, string passwordHash, bool mustChangePassword = true)
    {
        Validate(name, email, passwordHash);

        CompanyId = companyId;
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Active = true;
        MustChangePassword = mustChangePassword;
    }

    public bool IsLocked()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    public void SetPassword(string passwordHash)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(passwordHash), "Hash de senha não pode ser vazio.");

        PasswordHash = passwordHash;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public void RegisterAccess()
    {
        LastAccessAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    // Conta a falha e bloqueia temporariamente ao atingir o limite de tentativas.
    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxLoginAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            FailedLoginAttempts = 0;
        }
    }

    public void AssignRefreshToken(string refreshToken, DateTime expiresAt)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(refreshToken), "Refresh token não pode ser vazio.");

        RefreshToken = refreshToken;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public bool IsRefreshTokenValid(string refreshToken)
    {
        return !string.IsNullOrWhiteSpace(RefreshToken)
            && RefreshToken == refreshToken
            && RefreshTokenExpiresAt.HasValue
            && RefreshTokenExpiresAt.Value > DateTime.UtcNow;
    }

    public void Activate()
    {
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
        RevokeRefreshToken();
    }

    private static void Validate(string name, string email, string passwordHash)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(name), "Nome do usuário não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(email), "E-mail não pode ser vazio.");
        JotanunesException.When(!email.Contains('@') || !email.Contains('.'), "E-mail deve conter '@' e '.'");
        JotanunesException.When(string.IsNullOrWhiteSpace(passwordHash), "Hash de senha não pode ser vazio.");
    }
}
