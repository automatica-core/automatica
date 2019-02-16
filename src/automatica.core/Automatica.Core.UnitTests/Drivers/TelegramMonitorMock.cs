using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Core.UnitTests.Drivers
{
    public class TelegramMonitorMock : ITelegramMonitor
    {
        public void Clear()
        {
            // not needed in here
        }

        public ITelegramMonitorInstance CreateTelegramMonitor(Guid id, string name, string busType, string description)
        {
            return new EmptyTelegramMonitorInstance();
        }

        public ITelegramMonitorInstance CreateTelegramMonitor(NodeInstance instance, string busType)
        {
            return new EmptyTelegramMonitorInstance();
        }

        public IList<TelegramMonitorInstance> GetMonitorInstances()
        {
            return new List<TelegramMonitorInstance>();
        }

        public Task NotifyTelegram(ITelegramMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
