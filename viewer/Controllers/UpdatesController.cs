using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using viewer.Hubs;
using viewer.Models;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
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

        /// <summary>
        /// cloud event subscription validation
        /// </summary>
        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var requestHeaders = HttpContext.Request.Headers;
                var webhookRequestOrigin = requestHeaders["WebHook-Request-Origin"].FirstOrDefault();
                var webhookRequestCallback = requestHeaders["WebHook-Request-Callback"];
                var webhookRequestRate = requestHeaders["WebHook-Request-Rate"];

                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);

                var data = new GridUpdateModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "HttpOptions",
                    Time = DateTime.Now.ToString(),
                    Data = JsonConvert.SerializeObject(new
                    {
                        Method = "Options",
                        request = requestHeaders,
                    }, Formatting.Indented),
                };

                await SendMessage(data);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                // invalid content type
                if (string.IsNullOrEmpty(jsonContent) || !IsValidContentType(out bool isCloudEvent))
                {
                    return BadRequest();
                }

                try
                {
                    // resolve cloud event notifications
                    if (isCloudEvent)
                    {
                        return await HandleCloudEvents(jsonContent);
                    }

                    // fallback to resolve azure event grid notifications/subscription validation
                    var eventType = GetAzureEventType();
                    if (eventType == "Notification")
                    {
                        return await HandleGridEvents(jsonContent);
                    }
                    else if (eventType == "SubscriptionValidation")
                    {
                        return await HandleValidation(jsonContent);
                    }

                    // invalid content
                    return BadRequest();
                }
                catch (Exception ex)
                {
                    var data = new GridUpdateModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = ex.Message,
                        Time = DateTime.Now.ToString(),
                        Data = JsonConvert.SerializeObject(new
                        {
                            error = ex,
                            rawContent = JsonConvert.DeserializeObject(jsonContent),
                            request = Request.Headers,
                        }, Formatting.Indented),
                    };

                    await SendMessage(data);

                    return Problem(ex.Message);
                }
            }
        }

        #endregion

        #region Private Methods

        private string GetAzureEventType()
        {
            if (Request.Headers.ContainsKey("aeg-event-type"))
            {
                return HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault();
            }
            return null;
        }

        private async Task SendMessage(GridUpdateModel data, dynamic innerData = null)
        {
            await this._hubContext.Clients.All.GridUpdate(data);

            var subject = data.Subject;

            if (!string.IsNullOrEmpty(data.ETag))
            {
                data.Subject = string.Concat(data.ETag, "||", subject);
                await this._hubContext.Clients.Group(data.ETag).GridUpdate(data);
            }

            if (!string.IsNullOrEmpty(data.Session))
            {
                data.Subject = string.Concat(data.Session, "||", subject);
                await this._hubContext.Clients.Group(data.Session).GridUpdate(data);
            }
        }

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            IEvent<Dictionary<string, string>> gridEvent = 
                JsonConvert.DeserializeObject<IEnumerable<GridEvent<Dictionary<string, string>>>>(jsonContent).First();

            var data = GetGridUpdateModel(gridEvent, jsonContent, nameof(HandleValidation));
            await SendMessage(data);

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var detailCollection = JsonConvert.DeserializeObject<IEnumerable<GridEvent<dynamic>>>(jsonContent);
            foreach (var details in detailCollection)
            {
                var data = GetGridUpdateModel(details, jsonContent, nameof(HandleGridEvents));
                await SendMessage(data);
            }
            return Ok();
        }

        private async Task<IActionResult> HandleCloudEvents(string jsonContent)
        {
            if (jsonContent.TrimStart().StartsWith('['))
            {
                var detailCollection = JsonConvert.DeserializeObject<IEnumerable<CloudEvent<dynamic>>>(jsonContent);
                foreach(var details in detailCollection)
                {
                    var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents));
                    await SendMessage(data);
                }
            }
            else
            {
                var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
                var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents));
                await SendMessage(data);
            }

            return Ok();
        }

        private GridUpdateModel GetGridUpdateModel<T>(IEvent<T> details, string jsonContent, string method) where T : class
        {
            return new GridUpdateModel()
            {
                Id = details.Id,
                Type = details.Type,
                Subject = details.Subject,
                Time = details.Time.ToLongTimeString(),
                Data = JsonConvert.SerializeObject(new
                {
                    method = method,
                    itemContent = details,
                    rawContent = JsonConvert.DeserializeObject(jsonContent),
                    request = Request.Headers,
                }, Formatting.Indented),
            };
        }

        private GridUpdateModel GetGridUpdateModel(IEvent<dynamic> details, string jsonContent, string method)
        {
            var model = GetGridUpdateModel<dynamic>(details, jsonContent, method);
            if (details.Data != null)
            {
                model.ETag = details.Data.eTag;
                if (!string.IsNullOrEmpty(details.Type))
                {
                    if (details.Type == "Microsoft.Security.MalwareScanningResult")
                    {
                        model.Url = details.Data.blobUri;
                    }
                    else if (details.Type.StartsWith("Microsoft.Storage.Blob"))
                    {
                        model.Url = details.Data.url;
                    }
                    if (!string.IsNullOrEmpty(model.Url))
                    {
                        model.Session = Path.GetDirectoryName(new Uri(model.Url).LocalPath)
                            .Replace(@"\\", @"/")
                            .Trim('/');
                    }
                }
            }

            return model;
        }

        private bool IsValidContentType(out bool isCloudEvent)
        {
            var requestHeaders = Request.Headers;
            if (requestHeaders.ContainsKey(HeaderNames.ContentType))
            {
                var appContentType = requestHeaders.ContentType.FirstOrDefault(x => x.StartsWith("application/"))?.Substring(12);
                if (!string.IsNullOrEmpty(appContentType) && appContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    isCloudEvent = appContentType.StartsWith("cloudevents", StringComparison.OrdinalIgnoreCase);
                    return true;
                }
            }
            isCloudEvent = false;
            return false;
        }

        #endregion
    }
}