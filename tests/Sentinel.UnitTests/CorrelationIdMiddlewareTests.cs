using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Api.Middleware;
using Sentinel.Domain.Common;

namespace Sentinel.UnitTests.Api;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WellFormedHeader_IsKeptAndEchoed()
    {
        var (seen, context) = await RunAsync("abc-123_DEF.456:7");

        Assert.Equal("abc-123_DEF.456:7", seen);
        Assert.Equal("abc-123_DEF.456:7", context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_MissingHeader_GeneratesAnId()
    {
        var (seen, context) = await RunAsync(null);

        Assert.Matches("^[0-9a-f]{32}$", seen);
        Assert.Equal(seen, context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    [Theory]
    [InlineData("abc\r\n2026-01-01 ERROR forged entry")]
    [InlineData("abc\ndef")]
    [InlineData("has space")]
    public async Task InvokeAsync_HeaderThatCouldForgeLogEntries_IsReplaced(string header)
    {
        var (seen, context) = await RunAsync(header);

        Assert.NotEqual(header, seen);
        Assert.Matches("^[0-9a-f]{32}$", seen);
        Assert.Equal(seen, context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    private static async Task<(string? Seen, HttpContext Context)> RunAsync(string? header)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        if (header is not null)
        {
            context.Request.Headers[CorrelationId.HeaderName] = header;
        }

        string? seen = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            seen = ctx.Items[CorrelationId.HeaderName] as string;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        return (seen, context);
    }
}
