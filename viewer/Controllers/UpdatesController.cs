using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using viewer.Hubs;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
        #region Data Members

        private readonly IGridEventHubService _hub;

        #endregion

        #region Constructors

        public UpdatesController(IGridEventHubService hub)
        {
            this._hub = hub;
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

                await _hub.Broadcast("HttpOptions", content: Request.Headers);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    var jsonContent = await reader.ReadToEndAsync();
                    if (await _hub.Process(jsonContent, Request))
                    {
                        return Ok();
                    }
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                await _hub.Broadcast("Error", ex.Message, ex, Request);
                return Problem(ex.Message);
            }
        }

        #endregion

    }
}