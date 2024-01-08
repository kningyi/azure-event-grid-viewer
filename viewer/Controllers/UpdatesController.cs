using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using viewer.Hubs;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : HubControllerBase
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


        [HttpPost]
        public override async Task<IActionResult> Post()
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