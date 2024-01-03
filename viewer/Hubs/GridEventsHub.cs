using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    public class GridEventsHub : Hub<IGridEventsHubClient>
    {
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public GridEventsHub()
        {
            jsonSerializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };
        }

        public async Task SendMessage(string message)
        {
            await Clients.All.GridUpdate(new GridUpdateModel()
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Message",
                Subject = message,
                Time = DateTime.Now.ToString(),
                Data = JsonConvert.SerializeObject(new
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = Context.UserIdentifier,
                }, Formatting.Indented, jsonSerializerSettings),
            });
            ;
        }

        public async Task Subscribe(string subject)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, subject);
            await Clients.Group(subject).GridUpdate(new GridUpdateModel()
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Subject Subscription",
                Subject = subject,
                Time = DateTime.Now.ToString(),
                Data = JsonConvert.SerializeObject(new
                {
                    ConnectionId = Context.ConnectionId,
                    User = Context.User,
                    UserId = Context.UserIdentifier,
                    Features = Context.Features,
                }, Formatting.Indented, jsonSerializerSettings),
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
                Data = JsonConvert.SerializeObject(new
                {
                    ConnectionId = Context.ConnectionId,
                    User = Context.User,
                    UserId = Context.UserIdentifier,
                    Items = Context.Items,
                }, Formatting.Indented, jsonSerializerSettings),
            });
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, subject);
        }
    }
}