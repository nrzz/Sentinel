using FluentMigrator;

namespace Sentinel.Infrastructure.Persistence.PostgreSQL.Migrations;

[Migration(202607080001)]
public sealed class CreateMilestoneFiveSixTables : Migration
{
    public override void Up()
    {
        Create.Table("alert_rules")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("name").AsString(256).NotNullable()
            .WithColumn("description").AsString(2000).NotNullable()
            .WithColumn("query").AsString(int.MaxValue).NotNullable()
            .WithColumn("condition").AsString(1000).NotNullable()
            .WithColumn("severity").AsInt32().NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("evaluation_interval_seconds").AsInt64().NotNullable()
            .WithColumn("notification_channels").AsCustom("jsonb").NotNullable()
            .WithColumn("created_by").AsString(256).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("alert_executions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("alert_rule_id").AsGuid().NotNullable().Indexed()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("severity").AsInt32().NotNullable()
            .WithColumn("message").AsString(int.MaxValue).NotNullable()
            .WithColumn("matched_value").AsString(2000).Nullable()
            .WithColumn("triggered_at").AsDateTimeOffset().NotNullable()
            .WithColumn("resolved_at").AsDateTimeOffset().Nullable()
            .WithColumn("correlation_id").AsString(64).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.ForeignKey("fk_alert_executions_rule")
            .FromTable("alert_executions").ForeignColumn("alert_rule_id")
            .ToTable("alert_rules").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Table("alert_notifications")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("alert_execution_id").AsGuid().NotNullable().Indexed()
            .WithColumn("channel").AsString(128).NotNullable()
            .WithColumn("recipient").AsString(512).NotNullable()
            .WithColumn("subject").AsString(512).NotNullable()
            .WithColumn("body").AsString(int.MaxValue).NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("sent_at").AsDateTimeOffset().Nullable()
            .WithColumn("error_message").AsString(2000).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.ForeignKey("fk_alert_notifications_execution")
            .FromTable("alert_notifications").ForeignColumn("alert_execution_id")
            .ToTable("alert_executions").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Table("incidents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("title").AsString(512).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).NotNullable()
            .WithColumn("severity").AsInt32().NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("assigned_to").AsString(256).Nullable()
            .WithColumn("source_alert_execution_id").AsGuid().Nullable()
            .WithColumn("created_by").AsString(256).Nullable()
            .WithColumn("resolved_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("incident_timeline_entries")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("incident_id").AsGuid().NotNullable().Indexed()
            .WithColumn("entry_type").AsInt32().NotNullable()
            .WithColumn("message").AsString(int.MaxValue).NotNullable()
            .WithColumn("actor").AsString(256).Nullable()
            .WithColumn("metadata_json").AsCustom("jsonb").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.ForeignKey("fk_incident_timeline_incident")
            .FromTable("incident_timeline_entries").ForeignColumn("incident_id")
            .ToTable("incidents").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Table("incident_comments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("incident_id").AsGuid().NotNullable().Indexed()
            .WithColumn("author").AsString(256).NotNullable()
            .WithColumn("content").AsString(int.MaxValue).NotNullable()
            .WithColumn("is_internal").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.ForeignKey("fk_incident_comments_incident")
            .FromTable("incident_comments").ForeignColumn("incident_id")
            .ToTable("incidents").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Table("dashboards")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("name").AsString(256).NotNullable()
            .WithColumn("description").AsString(2000).NotNullable()
            .WithColumn("layout_json").AsCustom("jsonb").NotNullable()
            .WithColumn("is_default").AsBoolean().NotNullable()
            .WithColumn("created_by").AsString(256).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Table("plugins")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("name").AsString(256).NotNullable()
            .WithColumn("version").AsString(64).NotNullable()
            .WithColumn("description").AsString(2000).NotNullable()
            .WithColumn("assembly_name").AsString(512).NotNullable()
            .WithColumn("configuration_json").AsCustom("jsonb").NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("installed_by").AsString(256).Nullable()
            .WithColumn("installed_at").AsDateTimeOffset().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint("uq_plugins_tenant_name")
            .OnTable("plugins")
            .Columns("tenant_id", "name");

        Create.Table("ai_interactions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable().Indexed()
            .WithColumn("interaction_type").AsInt32().NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("provider").AsString(64).NotNullable()
            .WithColumn("model").AsString(128).NotNullable()
            .WithColumn("prompt_template_id").AsString(128).NotNullable()
            .WithColumn("prompt_template_version").AsString(32).NotNullable()
            .WithColumn("request_payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("response_payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("sources_json").AsCustom("jsonb").Nullable()
            .WithColumn("correlation_id").AsString(64).Nullable()
            .WithColumn("user_id").AsString(256).Nullable()
            .WithColumn("feedback_rating").AsInt32().Nullable()
            .WithColumn("feedback_comment").AsString(2000).Nullable()
            .WithColumn("latency_ms").AsInt64().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ai_interactions");
        Delete.Table("plugins");
        Delete.Table("dashboards");
        Delete.Table("incident_comments");
        Delete.Table("incident_timeline_entries");
        Delete.Table("incidents");
        Delete.Table("alert_notifications");
        Delete.Table("alert_executions");
        Delete.Table("alert_rules");
    }
}
