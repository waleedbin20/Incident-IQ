using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using IncidentIQ.Agents;
using IncidentIQ.Api.Hubs;

namespace IncidentIQ.Api.Services;

public class SignalRTraceBroadcaster : ITraceBroadcaster
{
    private readonly IHubContext<WarRoomHub> _hubContext;

    public SignalRTraceBroadcaster(IHubContext<WarRoomHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastTraceAsync(string level, string action, string message)
    {
        var trace = new
        {
            timestamp = System.DateTime.UtcNow.ToString("O"),
            level = level,
            action = action,
            message = message
        };
        await _hubContext.Clients.All.SendAsync("ReceiveTrace", trace);
    }
}
