using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Sentinel.Api.Hubs;

[Authorize]
public sealed class LogsHub : Hub
{
    public async Task Subscribe(string tenantId)
    {
        if (!Guid.TryParse(tenantId, out _))
        {
            throw new HubException("A valid tenant identifier is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
    }

    public async Task Unsubscribe(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
    }
}
