using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Shading.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    /// <summary>
    /// The automatic shading as the visualization sees it: the state of each configured blind, and switching its
    /// automatic on or off. Reading is open to every signed-in role, switching to visualization users and admins
    /// (the users that can move the blind anyway). The configuration stays in <see cref="ShadingController"/> (admins).
    /// </summary>
    [Route("webapi/shading-visu")]
    [Authorize(Policy = Role.ViewerRole)]
    public class ShadingVisuController : BaseController
    {
        private readonly IShadingEngine _engine;

        public ShadingVisuController(AutomaticaContext dbContext, IShadingEngine engine) : base(dbContext)
        {
            _engine = engine;
        }

        /// <summary>
        /// The configured blinds with mode and reason, keyed by control id
        /// </summary>
        [HttpGet("blinds")]
        public JToken GetBlinds()
        {
            var status = _engine.GetStatus();
            return ShadingController.ToJToken(status.Blinds.Select(a => new
            {
                a.ControlId,
                a.Mode,
                a.Reason,
                a.Demand,
                a.ManualUntil,
                Automatic = a.Mode != BlindMode.Paused
            }).ToList());
        }

        [HttpPost("blinds/{controlId:guid}/automatic")]
        [Authorize(Roles = Role.VisuRole + "," + Role.AdminRole)]
        public async Task<IActionResult> SetAutomatic(Guid controlId, [FromBody] ShadingAutomaticDto dto, CancellationToken token)
        {
            return await _engine.SetAutomaticAsync(controlId, dto?.Enabled ?? true, token) ? Ok() : NotFound();
        }

        /// <summary>
        /// Ends the manual override of a blind, the automatic takes over again
        /// </summary>
        [HttpPost("blinds/{controlId:guid}/resume")]
        [Authorize(Roles = Role.VisuRole + "," + Role.AdminRole)]
        public async Task<IActionResult> Resume(Guid controlId, CancellationToken token)
        {
            return await _engine.ResumeAsync(controlId, token) ? Ok() : NotFound();
        }
    }
}
