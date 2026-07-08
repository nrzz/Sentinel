using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private Role()
    {
    }

    public static Role Create(string name, string? description, bool isSystem = false)
    {
        return new Role
        {
            Name = name.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            IsSystem = isSystem
        };
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim().ToLowerInvariant();
        Description = description?.Trim();
        Touch();
    }
}
