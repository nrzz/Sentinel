using FluentMigrator;

namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

[Migration(1, "InitialSchema")]
public sealed class Migration_001_InitialSchema : Migration
{
    private static readonly string[] Schemas =
    [
        "identity",
        "tenant",
        "configuration",
        "alerts",
        "incidents",
        "audit",
        "plugins"
    ];

    public override void Up()
    {
        foreach (var schema in Schemas)
        {
            Execute.Sql($"CREATE SCHEMA IF NOT EXISTS {schema};");
        }

        CreateIdentityTables();
        CreateTenantTables();
        CreateConfigurationTables();
        CreateAlertsTables();
        CreateIncidentsTables();
        CreateAuditTables();
        CreatePluginsTables();
    }

    public override void Down()
    {
        Delete.Table("plugin_settings").InSchema("plugins");
        Delete.Table("plugin_registry").InSchema("plugins");
        Delete.Table("audit_events").InSchema("audit");
        Delete.Table("comments").InSchema("incidents");
        Delete.Table("timeline").InSchema("incidents");
        Delete.Table("incidents").InSchema("incidents");
        Delete.Table("notifications").InSchema("alerts");
        Delete.Table("executions").InSchema("alerts");
        Delete.Table("rules").InSchema("alerts");
        Delete.Table("settings").InSchema("configuration");
        Delete.Table("api_keys").InSchema("tenant");
        Delete.Table("members").InSchema("tenant");
        Delete.Table("tenants").InSchema("tenant");
        Delete.Table("refresh_tokens").InSchema("identity");
        Delete.Table("role_permissions").InSchema("identity");
        Delete.Table("user_roles").InSchema("identity");
        Delete.Table("permissions").InSchema("identity");
        Delete.Table("roles").InSchema("identity");
        Delete.Table("users").InSchema("identity");

        foreach (var schema in Schemas.Reverse())
        {
            Execute.Sql($"DROP SCHEMA IF EXISTS {schema} CASCADE;");
        }
    }

    private void CreateIdentityTables()
    {
        Create.Table("users").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(320).NotNullable().Unique()
            .WithColumn("password_hash").AsString(512).NotNullable()
            .WithColumn("display_name").AsString(200).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("email_verified").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("last_login_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("roles").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable().Unique()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("is_system").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("permissions").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable().Unique()
            .WithColumn("resource").AsString(100).NotNullable()
            .WithColumn("action").AsString(50).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("user_roles").InSchema("identity")
            .WithColumn("user_id").AsGuid().NotNullable()
                .ForeignKey("FK_user_roles_users", "identity", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("role_id").AsGuid().NotNullable()
                .ForeignKey("FK_user_roles_roles", "identity", "roles", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("tenant_id").AsGuid().NotNullable();

        Create.PrimaryKey("PK_user_roles")
            .OnTable("user_roles").WithSchema("identity")
            .Columns("user_id", "role_id", "tenant_id");

        Create.Index("IX_user_roles_tenant_id")
            .OnTable("user_roles").InSchema("identity")
            .OnColumn("tenant_id");

        Create.Table("role_permissions").InSchema("identity")
            .WithColumn("role_id").AsGuid().NotNullable()
                .ForeignKey("FK_role_permissions_roles", "identity", "roles", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("permission_id").AsGuid().NotNullable()
                .ForeignKey("FK_role_permissions_permissions", "identity", "permissions", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("PK_role_permissions")
            .OnTable("role_permissions").WithSchema("identity")
            .Columns("role_id", "permission_id");

        Create.Table("refresh_tokens").InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
                .ForeignKey("FK_refresh_tokens_users", "identity", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("token_hash").AsString(128).NotNullable().Unique()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
            .WithColumn("revoked_at").AsDateTimeOffset().Nullable()
            .WithColumn("replaced_by_token_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_refresh_tokens_user_id")
            .OnTable("refresh_tokens").InSchema("identity")
            .OnColumn("user_id");
    }

    private void CreateTenantTables()
    {
        Create.Table("tenants").InSchema("tenant")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable().Unique()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("environment").AsString(50).NotNullable().WithDefaultValue("production")
            .WithColumn("settings").AsCustom("jsonb").NotNullable().WithDefaultValue("'{}'")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("members").InSchema("tenant")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
                .ForeignKey("FK_members_tenants", "tenant", "tenants", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("user_id").AsGuid().NotNullable()
                .ForeignKey("FK_members_users", "identity", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("role_id").AsGuid().NotNullable()
                .ForeignKey("FK_members_roles", "identity", "roles", "id").OnDelete(System.Data.Rule.None)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("joined_at").AsDateTimeOffset().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint("UQ_members_tenant_user")
            .OnTable("members").WithSchema("tenant")
            .Columns("tenant_id", "user_id");

        Create.Index("IX_members_user_id")
            .OnTable("members").InSchema("tenant")
            .OnColumn("user_id");

        Create.Table("api_keys").InSchema("tenant")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
                .ForeignKey("FK_api_keys_tenants", "tenant", "tenants", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("key_hash").AsString(128).NotNullable().Unique()
            .WithColumn("key_prefix").AsString(12).NotNullable()
            .WithColumn("scopes").AsCustom("text[]").NotNullable().WithDefaultValue("'{}'")
            .WithColumn("expires_at").AsDateTimeOffset().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_by_user_id").AsGuid().Nullable()
                .ForeignKey("FK_api_keys_users", "identity", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("last_used_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_api_keys_tenant_id")
            .OnTable("api_keys").InSchema("tenant")
            .OnColumn("tenant_id");
    }

    private void CreateConfigurationTables()
    {
        Create.Table("settings").InSchema("configuration")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
                .ForeignKey("FK_settings_tenants", "tenant", "tenants", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("category").AsString(100).NotNullable()
            .WithColumn("key").AsString(200).NotNullable()
            .WithColumn("value").AsCustom("jsonb").NotNullable()
            .WithColumn("environment").AsString(50).NotNullable().WithDefaultValue("production")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint("UQ_settings_tenant_category_key_env")
            .OnTable("settings").WithSchema("configuration")
            .Columns("tenant_id", "category", "key", "environment");
    }

    private void CreateAlertsTables()
    {
        Create.Table("rules").InSchema("alerts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(2000).Nullable()
            .WithColumn("condition").AsCustom("jsonb").NotNullable()
            .WithColumn("severity").AsString(50).NotNullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("environment").AsString(50).NotNullable().WithDefaultValue("production")
            .WithColumn("created_by_user_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_rules_tenant_id")
            .OnTable("rules").InSchema("alerts")
            .OnColumn("tenant_id");

        Create.Table("executions").InSchema("alerts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("rule_id").AsGuid().NotNullable()
                .ForeignKey("FK_executions_rules", "alerts", "rules", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("triggered_at").AsDateTimeOffset().NotNullable()
            .WithColumn("resolved_at").AsDateTimeOffset().Nullable()
            .WithColumn("details").AsCustom("jsonb").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_executions_tenant_triggered")
            .OnTable("executions").InSchema("alerts")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("triggered_at").Descending();

        Create.Table("notifications").InSchema("alerts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("execution_id").AsGuid().NotNullable()
                .ForeignKey("FK_notifications_executions", "alerts", "executions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("channel").AsString(100).NotNullable()
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("sent_at").AsDateTimeOffset().Nullable()
            .WithColumn("details").AsCustom("jsonb").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_notifications_tenant_id")
            .OnTable("notifications").InSchema("alerts")
            .OnColumn("tenant_id");
    }

    private void CreateIncidentsTables()
    {
        Create.Table("incidents").InSchema("incidents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("description").AsString(4000).Nullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("open")
            .WithColumn("severity").AsString(50).NotNullable()
            .WithColumn("environment").AsString(50).NotNullable().WithDefaultValue("production")
            .WithColumn("assigned_to_user_id").AsGuid().Nullable()
            .WithColumn("created_by_user_id").AsGuid().Nullable()
            .WithColumn("resolved_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_incidents_tenant_created")
            .OnTable("incidents").InSchema("incidents")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("created_at").Descending();

        Create.Table("timeline").InSchema("incidents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("incident_id").AsGuid().NotNullable()
                .ForeignKey("FK_timeline_incidents", "incidents", "incidents", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("event_type").AsString(100).NotNullable()
            .WithColumn("content").AsString(4000).NotNullable()
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_timeline_incident_id")
            .OnTable("timeline").InSchema("incidents")
            .OnColumn("incident_id");

        Create.Table("comments").InSchema("incidents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("incident_id").AsGuid().NotNullable()
                .ForeignKey("FK_comments_incidents", "incidents", "incidents", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("user_id").AsGuid().NotNullable()
                .ForeignKey("FK_comments_users", "identity", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("content").AsString(4000).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_comments_incident_id")
            .OnTable("comments").InSchema("incidents")
            .OnColumn("incident_id");
    }

    private void CreateAuditTables()
    {
        Create.Table("audit_events").InSchema("audit")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().Nullable()
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("action").AsString(200).NotNullable()
            .WithColumn("resource_type").AsString(100).NotNullable()
            .WithColumn("resource_id").AsString(200).Nullable()
            .WithColumn("details").AsCustom("jsonb").NotNullable().WithDefaultValue("'{}'")
            .WithColumn("ip_address").AsString(45).Nullable()
            .WithColumn("user_agent").AsString(1000).Nullable()
            .WithColumn("environment").AsString(50).NotNullable().WithDefaultValue("production")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("IX_audit_events_tenant_created")
            .OnTable("audit_events").InSchema("audit")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("created_at").Descending();
    }

    private void CreatePluginsTables()
    {
        Create.Table("plugin_registry").InSchema("plugins")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().Nullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("version").AsString(50).NotNullable()
            .WithColumn("plugin_type").AsString(100).NotNullable()
            .WithColumn("assembly_path").AsString(1000).NotNullable()
            .WithColumn("is_enabled").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("metadata").AsCustom("jsonb").Nullable()
            .WithColumn("installed_at").AsDateTimeOffset().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint("UQ_plugin_registry_name_version_tenant")
            .OnTable("plugin_registry").WithSchema("plugins")
            .Columns("name", "version", "tenant_id");

        Create.Table("plugin_settings").InSchema("plugins")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("plugin_id").AsGuid().NotNullable()
                .ForeignKey("FK_plugin_settings_registry", "plugins", "plugin_registry", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("settings").AsCustom("jsonb").NotNullable().WithDefaultValue("'{}'")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint("UQ_plugin_settings_plugin_tenant")
            .OnTable("plugin_settings").WithSchema("plugins")
            .Columns("plugin_id", "tenant_id");
    }
}
