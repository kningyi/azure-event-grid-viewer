using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using viewer.Hubs;
using viewer.Models;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
        private const string EventTypeHeaderName = "aeg-event-type";
        private const string EventTypeSubscriptionValidation = "SubscriptionValidation";
        private const string EventTypeNotification = "Notification";

        #region Data Members

        private readonly IHubContext<GridEventsHub> _hubContext;

        #endregion

        #region Constructors

        public UpdatesController(IHubContext<GridEventsHub> gridEventsHubContext)
        {
            this._hubContext = gridEventsHubContext;
        }

        #endregion

        #region Public Methods

        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
                var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();
                var eventType = GetEventType();

                // Check the event type.
                // Return the validation code if it's 
                // a subscription validation request. 
                if (eventType == EventTypeSubscriptionValidation)
                {
                    return await HandleValidation(jsonContent);
                }
                else if (eventType == EventTypeNotification)
                {
                    // Check to see if this is passed in using
                    // the CloudEvents schema
                    if (IsCloudEvent(jsonContent))
                    {
                        return await HandleCloudEvent(jsonContent);
                    }

                    return await HandleGridEvents(jsonContent);
                }

                return BadRequest();                
            }
        }

        #endregion

        #region Private Methods

        private string? GetEventType()
        {
            if (Request.Headers.ContainsKey(EventTypeHeaderName))
            {
                return HttpContext.Request.Headers[EventTypeHeaderName].FirstOrDefault();
            }
            return null;
        }

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            var gridEvent =
                JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                    .First();

            var data = new
            {
                jsonContent = jsonContent,
                request = Request.Headers,
                response = Response.Headers,
                method = "HandleValidation",
            };

            await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                gridEvent.Id,
                gridEvent.EventType,
                gridEvent.Subject,
                gridEvent.EventTime.ToLongTimeString(),
                JsonConvert.SerializeObject(data)
                );
            ;

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var events = JArray.Parse(jsonContent);
            foreach (var e in events)
            {
                // Invoke a method on the clients for 
                // an event grid notiification.                 
                var details = JsonConvert.DeserializeObject<GridEvent<dynamic>>(e.ToString());

                var data = new
                {
                    itemContent = details,
                    jsonContent = jsonContent,
                    request = Request.Headers,
                    response = Response.Headers,
                    method = "HandleGridEvents",
                };

                await this._hubContext.Clients.All.SendAsync(
                    "gridupdate",
                    details.Id,
                    details.EventType,
                    details.Subject,
                    details.EventTime.ToLongTimeString(),
                    JsonConvert.SerializeObject(data)
                    );
            }

            return Ok();
        }

        private async Task<IActionResult> HandleCloudEvent(string jsonContent)
        {
            var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
            var eventData = JObject.Parse(jsonContent);

            var data = new
            {
                itemContent = details,
                jsonContent = jsonContent,
                request = Request.Headers,
                response = Response.Headers,
                method = "HandleCloudEvent",
            };

            await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                details.Id,
                details.Type,
                details.Subject,
                details.Time,
                JsonConvert.SerializeObject(data)
            );

            return Ok();
        }

        private static bool IsCloudEvent(string jsonContent)
        {
            // Cloud events are sent one at a time, while Grid events
            // are sent in an array. As a result, the JObject.Parse will 
            // fail for Grid events. 
            try
            {
                // Attempt to read one JSON object. 
                var eventData = JObject.Parse(jsonContent);

                // Check for the spec version property.
                var version = eventData["specversion"].Value<string>();
                if (!string.IsNullOrEmpty(version)) return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        #endregion
    }
}