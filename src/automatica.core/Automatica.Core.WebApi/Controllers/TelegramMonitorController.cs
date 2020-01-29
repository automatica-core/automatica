using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/telegramMonitor")]
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
