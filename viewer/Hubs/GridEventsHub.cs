using Microsoft.AspNetCore.SignalR;

namespace viewer.Hubs
{
    public class GridEventsHub : Hub<IGridEventsHubClient>
    {
    }
}