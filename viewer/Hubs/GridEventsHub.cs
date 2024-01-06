using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    internal class GridEventsHub : AbstractGridEventsHub
    {
        public override async Task BindSession(string folder, string sessionId = null)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Path.Combine(folder, string.Concat(new Random().Next().ToString("x"), DateTime.UtcNow.Ticks.ToString()))
                    .Replace('\\', '/')
                    .Trim('/');
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.Identification(new IdentityModel()
            {
                SessionId = sessionId,
                ConnectionId = Context.ConnectionId,
                User = Context.User?.Identity,
                UserId = Context.UserIdentifier,
            });
        }

        public override async Task Subscribe(string subject)
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

        public override async Task Unsubscribe(string subject)
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