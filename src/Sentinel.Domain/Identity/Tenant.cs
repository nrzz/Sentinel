using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class Tenant : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string Environment { get; private set; } = "production";
    public string SettingsJson { get; private set; } = "{}";

    private Tenant()
    {
    }

    public static Tenant Create(string name, string slug, string environment = "production")
    {
        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Environment = environment.Trim().ToLowerInvariant()
        };
    }

    public void Update(string name, string slug, bool isActive, string environment, string settingsJson)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        IsActive = isActive;
        Environment = environment.Trim().ToLowerInvariant();
        SettingsJson = string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson;
        Touch();
    }
}
