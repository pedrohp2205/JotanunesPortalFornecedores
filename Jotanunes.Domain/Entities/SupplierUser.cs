using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

// Usuário de acesso ao Portal do Fornecedor (frente externa).
// Sempre vinculado a uma empresa.
public class SupplierUser : BaseEntity
{
    public const int MaxLoginAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int PasswordResetTokenMinutes = 60;
    public const int PasswordResetCooldownMinutes = 2;

    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTime? LastAccessAt { get; private set; }
    public string SecurityStamp { get; private set; } = string.Empty;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetExpiresAt { get; private set; }

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
        SecurityStamp = NewSecurityStamp();
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
        RevokeAllSessions();
        ClearPasswordResetToken();
    }

    public void ResetPassword(string temporaryPasswordHash)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(temporaryPasswordHash), "Hash de senha não pode ser vazio.");

        SetPassword(temporaryPasswordHash);
        MustChangePassword = true;
    }

    public void AssignPasswordResetToken(string tokenHash)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(tokenHash), "Token de redefinição não pode ser vazio.");

        PasswordResetTokenHash = tokenHash;
        PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetTokenMinutes);
    }

    public bool IsPasswordResetTokenValid(string tokenHash)
    {
        return !string.IsNullOrWhiteSpace(PasswordResetTokenHash)
            && PasswordResetTokenHash == tokenHash
            && PasswordResetExpiresAt.HasValue
            && PasswordResetExpiresAt.Value > DateTime.UtcNow;
    }

    public bool CanRequestPasswordReset()
    {
        return !PasswordResetExpiresAt.HasValue
            || PasswordResetExpiresAt.Value.AddMinutes(-PasswordResetTokenMinutes + PasswordResetCooldownMinutes) <= DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetExpiresAt = null;
    }

    public void RegisterAccess()
    {
        LastAccessAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }
    
    public void RegisterFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxLoginAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            FailedLoginAttempts = 0;
        }
    }

    public void RevokeAllSessions()
    {
        SecurityStamp = NewSecurityStamp();
    }

    public void Activate()
    {
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
        RevokeAllSessions();
        ClearPasswordResetToken();
    }

    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static void Validate(string name, string email, string passwordHash)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(name), "Nome do usuário não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(email), "E-mail não pode ser vazio.");
        JotanunesException.When(!email.Contains('@') || !email.Contains('.'), "E-mail deve conter '@' e '.'");
        JotanunesException.When(string.IsNullOrWhiteSpace(passwordHash), "Hash de senha não pode ser vazio.");
    }
}
