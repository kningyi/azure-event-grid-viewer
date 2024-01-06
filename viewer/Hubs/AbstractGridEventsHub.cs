using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace viewer.Hubs
{
    public abstract class AbstractGridEventsHub : Hub<IGridEventsHubClient>
    {
        public abstract Task BindSession(string folder, string sessionId = null);

        public abstract Task Subscribe(string subject);

        public abstract Task Unsubscribe(string subject);
    }
}
