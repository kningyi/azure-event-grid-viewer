using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    public class GridEventsHub : Hub<IGridEventsHubClient>
    {
        public async Task Subscribe(string subject)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, subject);
            await Clients.Group(subject).GridUpdate(new GridUpdateModel()
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Subject Subscription",
                Subject = subject,
                Time = DateTime.Now.ToString(),
                Data = JsonConvert.SerializeObject(new IdentityModel()
                {
                    ConnectionId = Context.ConnectionId,
                    User = Context.User?.Identity,
                    UserId = Context.UserIdentifier,
                }, Formatting.Indented),
            });
        }

        public async Task Unsubscribe(string subject)
        {
            await Clients.Group(subject).GridUpdate(new GridUpdateModel()
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Subject Unsubscription",
                Subject = subject,
                Time = DateTime.Now.ToString(),
                Data = JsonConvert.SerializeObject(new IdentityModel()
                {
                    ConnectionId = Context.ConnectionId,
                    User = Context.User?.Identity,
                    UserId = Context.UserIdentifier,
                }, Formatting.Indented),
            });
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, subject);
        }
    }
}