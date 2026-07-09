using FluentValidation;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Sentinel.Api.Authentication;
using Sentinel.Api.Authorization;
using Sentinel.Api.Configuration;
using Sentinel.Api.Features.AI;
using Sentinel.Api.Features.Alerts;
using Sentinel.Api.Features.Authentication;
using Sentinel.Api.Features.Dashboards;
using Sentinel.Api.Features.Deployments;
using Sentinel.Api.Features.Incidents;
using Sentinel.Api.Features.Logs;
using Sentinel.Api.Features.Metrics;
using Sentinel.Api.Features.Plugins;
using Sentinel.Api.Features.Search;
using Sentinel.Api.Features.Tenants;
using Sentinel.Api.Features.Traces;
using Sentinel.Api.Features.Users;
using Sentinel.Api.Hubs;
using Sentinel.Api.Middleware;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure;
using Sentinel.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSentinelInfrastructure(builder.Configuration);
builder.Services.AddAuthenticationFeatures(builder.Configuration);
builder.Services.AddUserFeatures();
builder.Services.AddTenantFeatures();
builder.Services.AddLogFeatures();
builder.Services.AddSearchFeatures();
builder.Services.AddMetricFeatures();
builder.Services.AddTraceFeatures();
builder.Services.AddAlertFeatures();
builder.Services.AddIncidentFeatures();
builder.Services.AddDeploymentFeatures();
builder.Services.AddDashboardFeatures();
builder.Services.AddPluginFeatures();
builder.Services.AddAiFeatures(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: true));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.AuthenticationScheme,
        _ => { });

builder.Services.AddSentinelAuthorization();

var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (securityOptions.AllowedCorsOrigins.Length > 0)
        {
            policy.WithOrigins(securityOptions.AllowedCorsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

var sentinelOptions = builder.Configuration.GetSection(SentinelOptions.SectionName).Get<SentinelOptions>()!;
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(sentinelOptions.ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Services.AddSignalR();
builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Sentinel API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});

ProductionConfigValidator.Validate(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .WithName("Health")
    .WithTags("Health")
    .AllowAnonymous();

app.MapHealthChecks("/ready", new() { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous();
app.MapHealthChecks("/live", new() { Predicate = _ => false })
    .AllowAnonymous();

app.MapAuthenticationEndpoints();
app.MapUserEndpoints();
app.MapTenantEndpoints();
app.MapLogEndpoints();
app.MapSearchEndpoints();
app.MapMetricEndpoints();
app.MapTraceEndpoints();
app.MapAlertEndpoints();
app.MapIncidentEndpoints();
app.MapDeploymentEndpoints();
app.MapDashboardEndpoints();
app.MapPluginEndpoints();
app.MapAiEndpoints();

app.MapGrpcService<LogIngestionGrpcService>();
app.MapGrpcService<MetricsIngestionGrpcService>();
app.MapGrpcService<TraceIngestionGrpcService>();
app.MapHub<LogsHub>("/hubs/logs");
app.MapHub<AlertsHub>("/hubs/alerts");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var topology = scope.ServiceProvider.GetRequiredService<IRabbitMqTopologyInitializer>();
    await topology.InitializeAsync();

    var migrator = scope.ServiceProvider.GetRequiredService<Sentinel.Infrastructure.Persistence.PostgreSQL.IMigrationRunner>();
    await migrator.MigrateAsync();

    var clickHouseMigrator = scope.ServiceProvider.GetRequiredService<Sentinel.Infrastructure.Persistence.ClickHouse.IClickHouseMigrator>();
    await clickHouseMigrator.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<Sentinel.Infrastructure.Persistence.PostgreSQL.IDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;
