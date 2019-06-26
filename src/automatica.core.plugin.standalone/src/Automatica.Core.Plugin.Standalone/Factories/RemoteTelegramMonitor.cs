using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteTelegramMonitor : ITelegramMonitor
    {
        public ITelegramMonitorInstance CreateTelegramMonitor(Guid id, string name, string type, string description)
        {
            return new RemoteTelegramMonitorInstance();
        }

        public ITelegramMonitorInstance CreateTelegramMonitor(NodeInstance instance, string type)
        {
            return new RemoteTelegramMonitorInstance();
        }

        public IList<TelegramMonitorInstance> GetMonitorInstances()
        {
            return new List<TelegramMonitorInstance>();
        }

        public Task NotifyTelegram(ITelegramMessage message)
        {
            return Task.CompletedTask;
        }

        public void Clear()
        {
        }
    }
}
