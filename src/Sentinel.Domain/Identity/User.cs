using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool EmailVerified { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User()
    {
    }

    public static User Create(string email, string passwordHash, string displayName)
    {
        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim()
        };
    }

    public void UpdateProfile(string displayName, bool isActive)
    {
        DisplayName = displayName.Trim();
        IsActive = isActive;
        Touch();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        Touch();
    }
}
