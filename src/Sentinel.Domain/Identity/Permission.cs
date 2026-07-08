using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class Permission : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Permission()
    {
    }

    public static Permission Create(string name, string resource, string action, string? description = null)
    {
        return new Permission
        {
            Name = name.Trim().ToLowerInvariant(),
            Resource = resource.Trim().ToLowerInvariant(),
            Action = action.Trim().ToLowerInvariant(),
            Description = description?.Trim()
        };
    }
}
