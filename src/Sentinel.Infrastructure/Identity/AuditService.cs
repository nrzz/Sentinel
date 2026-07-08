using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Sentinel.Domain.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class AuditService : IAuditService
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        IPostgreSqlConnectionFactory connectionFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _connectionFactory = connectionFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string resourceType,
        string? resourceId,
        Guid? tenantId,
        Guid? userId,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var detailsJson = details is null ? "{}" : JsonSerializer.Serialize(details);
        var auditEvent = AuditEvent.Create(
            action,
            resourceType,
            resourceId,
            tenantId,
            userId,
            detailsJson,
            ipAddress,
            userAgent);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO audit.audit_events
                (id, tenant_id, user_id, action, resource_type, resource_id, details, ip_address, user_agent, environment, created_at, updated_at)
            VALUES
                (@Id, @TenantId, @UserId, @Action, @ResourceType, @ResourceId, @Details::jsonb, @IpAddress, @UserAgent, @Environment, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                auditEvent.Id,
                auditEvent.TenantId,
                auditEvent.UserId,
                auditEvent.Action,
                auditEvent.ResourceType,
                auditEvent.ResourceId,
                Details = auditEvent.DetailsJson,
                auditEvent.IpAddress,
                auditEvent.UserAgent,
                auditEvent.Environment,
                auditEvent.CreatedAt,
                auditEvent.UpdatedAt
            });
    }
}
