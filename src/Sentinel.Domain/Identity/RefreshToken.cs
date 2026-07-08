using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(Guid? replacedByTokenId = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
        Touch();
    }
}
