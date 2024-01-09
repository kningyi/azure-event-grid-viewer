using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    internal class GridEventHubService : IGridEventHubService
    {
        #region Data Members

        private readonly IHubContext<AbstractFileStorageHub, IFileStorageHubClient> _hubContext;

        #endregion

        #region Constructors

        public GridEventHubService(IHubContext<AbstractFileStorageHub, IFileStorageHubClient> hubContext)
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
                return await HandleCloudEvents(jsonContent, request);
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
            var items = JsonConvert.DeserializeObject<IEnumerable<GridEvent<dynamic>>>(jsonContent);
            foreach (var details in items)
            {
                string url = null;
                string etag = null;
                if (details.Data != null)
                {
                    if (details.Type == "Microsoft.Security.MalwareScanningResult")
                    {
                        url = details.Data.blobUri;
                        etag = details.Data.eTag;
                    }
                    else if (details.Type.StartsWith("Microsoft.Storage."))
                    {
                        url = details.Data.url;
                        etag = details.Data.eTag;
                    }
                }
                var data = GetGridUpdateModel<dynamic>(details, jsonContent, nameof(HandleGridEvents), request, etag, url);
                await SendMessage(data);
            }
        }

        private async Task<bool> HandleCloudEvents(string jsonContent, HttpRequest request)
        {
            if (jsonContent.TrimStart().StartsWith('['))
            {
                var items = JArray.Parse(jsonContent);
                foreach (var item in items)
                {
                    var snippet = item.ToObject<CloudEventSnippet>();
                    if (snippet == null || string.IsNullOrEmpty(snippet.Type))
                    {
                        return false;
                    }
                    if (snippet.Type == "Microsoft.Security.MalwareScanningResult")
                    {
                        var details = item.ToObject<CloudEvent<ScanResult>>();
                        return await ProcessScanResultEvent(details, jsonContent);
                    }
                    else if (snippet.Type.StartsWith("Microsoft.Storage."))
                    {
                        var details = item.ToObject<CloudEvent<dynamic>>();
                        var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents), request, details.Data?.eTag, details.Data?.url);
                        await SendMessage(data);
                        return true;
                    }
                }
            }
            else
            {
                var snippet = JsonConvert.DeserializeObject<CloudEventSnippet>(jsonContent);
                if (snippet.Type == "Microsoft.Security.MalwareScanningResult")
                {
                    var details = JsonConvert.DeserializeObject<CloudEvent<ScanResult>>(jsonContent);
                    return await ProcessScanResultEvent(details, jsonContent);
                }
                else if (snippet.Type.StartsWith("Microsoft.Storage."))
                {
                    var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
                    var data = GetGridUpdateModel(details, jsonContent, nameof(HandleCloudEvents), request, details.Data?.eTag, details.Data?.url);
                    await SendMessage(data);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> ProcessScanResultEvent<T>(T details, string jsonContent) where T : class, IEvent<ScanResult>
        {
            if (details?.Data == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(details.Data.Url))
            {
                return false;
            }
            var uri = new Uri(details.Data.Url);
            var data = new ScanResultDto()
            {
                Id = details.Data.CorrelationId,
                ETag = details.Data.ETag,
                Url = uri.LocalPath,
                FinishedTime = details.Data.FinishedTime,
                Passed = details.Data.Result == "No threats found",
            };
            var passedData = new GridUpdateModel()
            {
                Id = details.Id,
                Type = details.Type,
                Subject = details.Subject,
                Time = details.Time.ToLongTimeString(),
                ETag = data.ETag,
                Url = data.Url,
                Subscription = GetSubscription(data.Url),
                Data = JsonConvert.SerializeObject(new
                {
                    method = "Handle " + typeof(T).Name,
                    data = data,
                    itemContent = details,
                    rawContent = JsonConvert.DeserializeObject(jsonContent),
                }, Formatting.Indented),
            };
            await SendMessage(passedData);
            return true;
        }

        private GridUpdateModel GetGridUpdateModel<T>(IEvent<T> details, string jsonContent, string method, HttpRequest request, string etag = null, string url = null) where T : class
        {
            return new GridUpdateModel()
            {
                Id = details.Id,
                Type = details.Type,
                Subject = details.Subject,
                Time = details.Time.ToLongTimeString(),
                ETag = etag,
                Url = url,
                Subscription = GetSubscription(url),
                Data = JsonConvert.SerializeObject(new
                {
                    method = method,
                    itemContent = details,
                    rawContent = JsonConvert.DeserializeObject(jsonContent),
                    request = request?.Headers,
                }, Formatting.Indented),
            };
        }

        private string GetSubscription(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            var uri = new Uri(url);
            if (uri.Segments.Length < 4)
            {
                return "/";
            }
            return string.Join("", uri.Segments.Skip(2).Take(uri.Segments.Length - 3)).Trim('/');
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

            if (!string.IsNullOrEmpty(data.Subscription))
            {
                data.Subject = string.Concat(data.Subscription, "||", subject);
                await this._hubContext.Clients.Group(data.Subscription).GridUpdate(data);
            }
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
