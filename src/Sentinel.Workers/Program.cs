using Sentinel.Infrastructure;
using Sentinel.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AlertEvaluationOptions>(
    builder.Configuration.GetSection(AlertEvaluationOptions.SectionName));

builder.Services.AddSentinelInfrastructure(builder.Configuration);
builder.Services.AddHostedService<LogEnrichmentWorker>();
builder.Services.AddHostedService<LogPersistenceWorker>();
builder.Services.AddHostedService<AlertEvaluationWorker>();

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

var host = builder.Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Sentinel.Workers");
    logger.LogInformation("Sentinel workers shutting down gracefully");
});

await host.RunAsync();
