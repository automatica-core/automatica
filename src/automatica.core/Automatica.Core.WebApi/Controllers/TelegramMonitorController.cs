using System.Collections.Generic;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/telegramMonitor")]
    [Authorize(Policy = Role.AdminRole)]
    public class TelegramMonitorController : BaseController
    {
        private readonly ITelegramMonitor _telegramMonitor;

        public TelegramMonitorController(AutomaticaContext dbContext, ITelegramMonitor telegramMonitor) : base(dbContext)
        {
            _telegramMonitor = telegramMonitor;
        }

        [Route("")]
        [HttpGet]
        public IList<TelegramMonitorInstance> GetTelegramMonitors()
        {
            return _telegramMonitor.GetMonitorInstances();
        }
    }
}
