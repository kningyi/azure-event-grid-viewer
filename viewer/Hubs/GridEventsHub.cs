using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    public class GridEventsHub : Hub<IGridEventsHubClient>
    {
        public async Task BindSession(string sessionId = null)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N").Substring(0, 6) + DateTime.UtcNow.Ticks.ToString();
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.Identification(new IdentityModel()
            {
                SessionId = sessionId,
                ConnectionId = Context.ConnectionId,
                User = Context.User?.Identity,
            });
        }
    }
}