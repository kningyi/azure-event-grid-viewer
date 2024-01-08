using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace viewer.Controllers
{
    public abstract class HubControllerBase : ControllerBase
    {
        /// <summary>
        /// cloud event subscription validation
        /// </summary>
        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            var requestHeaders = HttpContext.Request.Headers;
            var webhookRequestOrigin = requestHeaders["WebHook-Request-Origin"].FirstOrDefault();
            var webhookRequestCallback = requestHeaders["WebHook-Request-Callback"];
            var webhookRequestRate = requestHeaders["WebHook-Request-Rate"];

            HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
            HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            return Ok();
        }

        [HttpPost]
        public abstract Task<IActionResult> Post();
    }
}
