using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Sentinel.Sdk;

public static class SentinelServiceCollectionExtensions
{
    public static IServiceCollection AddSentinelClient(
        this IServiceCollection services,
        Action<SentinelClientOptions> configure)
    {
        var options = new SentinelClientOptions();
        configure(options);
        services.AddSingleton(options);

        services.AddHttpClient<SentinelClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
        })
        .AddPolicyHandler(GetRetryPolicy(options.MaxRetryAttempts));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetryAttempts) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(maxRetryAttempts, retry => TimeSpan.FromMilliseconds(Math.Pow(2, retry) * 100));
}
