using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    internal class GridEventHubService : IGridEventHubService
    {
        #region Data Members

        private readonly IHubContext<AbstractGridEventsHub, IGridEventsHubClient> _hubContext;

        #endregion

        #region Constructors

        public GridEventHubService(IHubContext<AbstractGridEventsHub, IGridEventsHubClient> hubContext)
        {
            this._hubContext = hubContext;
        }

        #endregion

        #region Public Methods

        public async Task Broadcast<T>(string type, string subject = null, T content = null, HttpRequest request = null) where T : class
        {
            var data = new GridUpdateModel()
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Subject = subject,
                Time = DateTime.Now.ToString(),
                Data = JsonConvert.SerializeObject(new
                {
                    Content = content,
                    request = request?.Headers,
                }, Formatting.Indented),
            };

            await SendMessage(data);
        }

        public async Task<bool> Process(string jsonContent, HttpRequest request)
        {
            // invalid content type
            if (string.IsNullOrEmpty(jsonContent) || !IsValidContentType(request, out bool isCloudEvent))
            {
                return false;
            }

            // resolve cloud event notifications
            if (isCloudEvent)
            {
                await HandleCloudEvents(jsonContent, request);
                return true;
            }

            // fallback to resolve azure event grid notifications/subscription validation
            var eventType = GetAzureEventType(request);
            if (eventType == "Notification")
            {
                await HandleGridEvents(jsonContent, request);
                return true;
            }
            else if (eventType == "SubscriptionValidation")
            {
                await HandleValidation(jsonContent, request);
            }

            // invalid content
            return false;
        }

        #endregion

        #region Private Methods

        private string GetAzureEventType(HttpRequest request)
        {
            if (request.Headers.ContainsKey("aeg-event-type"))
            {
                return request.Headers["aeg-event-type"].FirstOrDefault();
            }
            return null;
        }

        private async Task SendMessage(GridUpdateModel data)
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

        private async Task<JsonResult> HandleValidation(string jsonContent, HttpRequest request)
        {
            IEvent<Dictionary<string, string>> gridEvent =
                JsonConvert.DeserializeObject<IEnumerable<GridEvent<Dictionary<string, string>>>>(jsonContent).First();

            var data = GetGridUpdateModel(gridEvent, jsonContent, nameof(HandleValidation), request);
            await SendMessage(data);

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task HandleGridEvents(string jsonContent, HttpRequest request)
        {
            var detailCollection = JsonConvert.DeserializeObject<IEnumerable<GridEvent<dynamic>>>(jsonContent);
            foreach (var details in detailCollection)
            {
                var data = GetGridUpdateModel(details, jsonContent, nameof(HandleGridEvents), request);
                await SendMessage(data);
            }
        }

        private async Task HandleCloudEvents(string jsonContent, HttpRequest request)
        {
            if (jsonContent.TrimStart().StartsWith('['))
            {
                var detailCollection = JsonConvert.DeserializeObject<IEnumerable<CloudEvent<object>>>(jsonContent);
                foreach (var details in detailCollection)
                {
                    var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents), request);
                    await SendMessage(data);
                }
            }
            else
            {
                var details = JsonConvert.DeserializeObject<CloudEvent<object>>(jsonContent);
                var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents), request);
                await SendMessage(data);
            }
        }

        private GridUpdateModel GetGridUpdateModel<T>(IEvent<T> details, string jsonContent, string method, HttpRequest request) where T : class
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
                    request = request?.Headers,
                }, Formatting.Indented),
            };
        }

        private GridUpdateModel GetGridUpdateModel(IEvent<object> details, string jsonContent, string method, HttpRequest request)
        {
            var model = GetGridUpdateModel<object>(details, jsonContent, method, request);
            if (details.Data != null)
            {
                if (!string.IsNullOrEmpty(details.Type))
                {

                    if (details.Type == "Microsoft.Security.MalwareScanningResult")
                    {
                        var innerData = details.Data as ScanResultDto;
                        model.ETag = innerData.ETag;
                        model.Url = innerData.Url;
                    }
                    else if (details.Type.StartsWith("Microsoft.Storage.Blob"))
                    {
                        var innerData = details.Data as dynamic;
                        model.ETag = innerData.eTag;
                        model.Url = innerData.url;
                    }
                    if (!string.IsNullOrEmpty(model.Url))
                    {
                        model.Session = Path.GetDirectoryName(new Uri(model.Url).LocalPath)
                            .Replace('\\', '/')
                            .Trim('/');
                    }
                }
            }

            return model;
        }

        private bool IsValidContentType(HttpRequest request, out bool isCloudEvent)
        {
            var requestHeaders = request.Headers;
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
