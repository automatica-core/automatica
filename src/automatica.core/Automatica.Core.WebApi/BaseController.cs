using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.WebApi
{
    public abstract class BaseController : ControllerBase
    {
        public AutomaticaContext DbContext { get; }
        protected BaseController(AutomaticaContext dbContext)
        {
            DbContext = dbContext;
        }

        public bool IsUserInGroup(Guid? groupId)
        {
            if(groupId == null)
            {
                return true;
            }
            var claim = User?.Claims.FirstOrDefault(a => a.Type == UserGroup.ClaimType && a.Value == groupId.ToString());
            return claim != null;
        }

        /// <summary>
        /// Like <see cref="IsUserInGroup"/>, but administrators may access everything (like the MCP access rules).
        /// Used for the viewer endpoints that return single nodes, pages or values.
        /// </summary>
        public bool CanAccess(Guid? groupId)
        {
            return groupId == null || IsAdmin || IsUserInGroup(groupId);
        }

        protected bool IsAdmin => User?.IsInRole(Role.AdminRole) ?? false;

        /// <summary>
        /// Stops the server gracefully (the running requests, this one included, are completed) with the given exit code,
        /// the watchdog evaluates it (restart, install update, ...).
        /// </summary>
        protected void StopServer(int exitCode)
        {
            Environment.ExitCode = exitCode;
            var lifetime = HttpContext?.RequestServices?.GetService<IHostApplicationLifetime>();
            if (lifetime == null)
            {
                Environment.Exit(exitCode);
                return;
            }

            lifetime.StopApplication();
        }
    }
}
