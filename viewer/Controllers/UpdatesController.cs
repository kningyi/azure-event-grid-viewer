using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private readonly IHubContext<GridEventsHub, IGridEventsHubClient> _hubContext;

        #endregion

        #region Constructors

        public UpdatesController(IHubContext<GridEventsHub, IGridEventsHubClient> hubContext)
        {
            this._hubContext = hubContext;
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
                    if (IsCloudEvent())
                    {
                        return await HandleValidationForCloudEvent(jsonContent);
                    }
                    return await HandleValidation(jsonContent);
                }
                else if (eventType == EventTypeNotification)
                {
                    // Check to see if this is passed in using
                    // the CloudEvents schema
                    if (IsCloudEvent())
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
            IEvent<Dictionary<string, string>> gridEvent = 
                JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent).First();

            var data = new
            {
                method = "HandleValidation",
                itemContent = gridEvent,
                rawContent = JsonConvert.DeserializeObject(jsonContent),
                request = Request.Headers,
            };

            await this._hubContext.Clients.All.GridUpdate(
                new GridUpdateModel()
                {
                    Id = gridEvent.Id,
                    Type = gridEvent.Type,
                    Subject = gridEvent.Subject,
                    Time = gridEvent.Time.ToLongTimeString(),
                    Data = JsonConvert.SerializeObject(data, Formatting.Indented)
                }
            );

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<JsonResult> HandleValidationForCloudEvent(string jsonContent)
        {
            var gridEvent = jsonContent.TrimStart().StartsWith("[")
                ? JsonConvert.DeserializeObject<List<CloudEvent<Dictionary<string, string>>>>(jsonContent).First()
                : JsonConvert.DeserializeObject<CloudEvent<Dictionary<string, string>>>(jsonContent)
                ;

            var data = new
            {
                method = "HandleValidation",
                itemContent = gridEvent,
                rawContent = JsonConvert.DeserializeObject(jsonContent),
                request = Request.Headers,
            };

            await this._hubContext.Clients.All.GridUpdate(
                new GridUpdateModel()
                {
                    Id = gridEvent.Id,
                    Type = gridEvent.Type,
                    Subject = gridEvent.Subject,
                    Time = gridEvent.Time.ToLongTimeString(),
                    Data = JsonConvert.SerializeObject(data, Formatting.Indented)
                }
            );

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
                    method = "HandleGridEvents",
                    itemContent = details,
                    rawContent = JsonConvert.DeserializeObject(jsonContent),
                    request = Request.Headers,
                };

                await this._hubContext.Clients.All.GridUpdate(
                    new GridUpdateModel()
                    {
                        Id = details.Id,
                        Type = details.Type,
                        Subject = details.Subject,
                        Time = details.Time.ToLongTimeString(),
                        Data = JsonConvert.SerializeObject(data, Formatting.Indented)
                    }
                );
            }

            return Ok();
        }

        private async Task<IActionResult> HandleCloudEvent(string jsonContent)
        {
            var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);

            var data = new
            {
                method = "HandleCloudEvent",
                itemContent = details,
                rawContent = JsonConvert.DeserializeObject(jsonContent),
                request = Request.Headers,
            };

            await this._hubContext.Clients.All.GridUpdate(
                new GridUpdateModel()
                {
                    Id = details.Id,
                    Type = details.Type,
                    Subject = details.Subject,
                    Time = details.Time.ToLongTimeString(),
                    Data = JsonConvert.SerializeObject(data, Formatting.Indented)
                }
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

        private bool IsCloudEvent()
        {
            var requestHeaders = Request.Headers;
            if (requestHeaders.ContainsKey(HeaderNames.ContentType))
            {
                return requestHeaders.ContentType.Any(x => x.StartsWith("application/cloudevents", StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        #endregion
    }
}