using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    public class SecurityCommandDto
    {
        public string Command { get; set; }

        /// <summary>
        /// Optional arguments, may hold codes: never logged
        /// </summary>
        public Dictionary<string, string> Arguments { get; set; }
    }

    public class SecurityControlStateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public IReadOnlyCollection<string> Commands { get; set; }
        public bool CanCommand { get; set; }
        public bool IsAdmin { get; set; }
        public object State { get; set; }
    }

    /// <summary>
    /// The security logics (burglar, fire/water and emergency alarm, alarm chain, access controller) as the visualization sees
    /// them. Reading is open to every signed-in role that may see the logic (user group); arming, disarming and acknowledging
    /// need the roles visu or administrator. The logic itself refuses its management commands (e.g. access credentials) to
    /// non-admins. Every command is logged with the user, never with its arguments.
    /// </summary>
    [Route("webapi/security-visu")]
    [Authorize(Policy = Role.ViewerRole)]
    public class SecurityVisuController : BaseController
    {
        /// <summary>
        /// The roles that may send commands (arm, disarm, acknowledge...) to a security logic
        /// </summary>
        public const string CommandRoles = Role.VisuRole + "," + Role.AdminRole;

        private readonly IControlCache _controlCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly ILogger<SecurityVisuController> _logger;

        public SecurityVisuController(AutomaticaContext dbContext, IControlCache controlCache, ILogicInstanceCache logicInstanceCache,
            ILogger<SecurityVisuController> logger) : base(dbContext)
        {
            _controlCache = controlCache;
            _logicInstanceCache = logicInstanceCache;
            _logger = logger;
        }

        private ISecurityControl Find(Guid controlId)
        {
            if (_controlCache.Get(controlId) is not ISecurityControl control)
            {
                return null;
            }

            // logics of other user groups look like unknown ids
            var logic = _logicInstanceCache.Get(controlId);
            if (logic != null && !CanAccess(logic.This2UserGroup))
            {
                return null;
            }

            return control;
        }

        [HttpGet("{controlId:guid}")]
        public IActionResult GetState(Guid controlId)
        {
            var control = Find(controlId);
            if (control == null)
            {
                return NotFound();
            }

            return Ok(new SecurityControlStateDto
            {
                Id = control.Id,
                Name = control.Name,
                Kind = control.SecurityKind,
                Commands = control.SecurityCommands,
                CanCommand = User.IsInRole(Role.VisuRole) || IsAdmin,
                IsAdmin = IsAdmin,
                State = control.GetSecurityState(IsAdmin)
            });
        }

        [HttpPost("{controlId:guid}/command")]
        [Authorize(Roles = CommandRoles)]
        public async Task<IActionResult> Command(Guid controlId, [FromBody] SecurityCommandDto dto, CancellationToken token)
        {
            var control = Find(controlId);
            if (control == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(dto?.Command))
            {
                return BadRequest();
            }

            var userName = User?.Identity?.Name ?? "?";
            var context = new SecurityCommandContext(userName, IsAdmin, DateTimeHelper.ProviderInstance.GetUtcNow());
            var result = await control.ExecuteSecurityCommandAsync(dto.Command, dto.Arguments, context, token);

            // audit trail: who did what, never the arguments (codes)
            _logger.LogInformation("Security command {Command} on {ControlName} ({ControlId}) by {User}: {Result}",
                Sanitize(dto.Command), control.Name, control.Id, Sanitize(userName), result);

            return result switch
            {
                SecurityCommandResult.Ok => Ok(new { state = control.GetSecurityState(IsAdmin) }),
                SecurityCommandResult.UnknownCommand => BadRequest(),
                SecurityCommandResult.Denied => Forbid(),
                _ => Conflict(new { state = control.GetSecurityState(IsAdmin) })
            };
        }

        private static string Sanitize(string value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var cleaned = new string(Array.ConvertAll(value.ToCharArray(), c => char.IsControl(c) ? '_' : c));
            return cleaned.Length > 64 ? cleaned.Substring(0, 64) + "..." : cleaned;
        }
    }
}
