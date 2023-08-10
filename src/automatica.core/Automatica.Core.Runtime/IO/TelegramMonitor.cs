using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public class TelegramMonitor : ITelegramMonitor
    {
        private readonly IHubContext<TelegramHub> _telegramHub;
        private readonly Dictionary<Guid, TelegramMonitorInstance> _telegramMonitorInstances;

        public TelegramMonitor(IHubContext<TelegramHub> telegramHub)
        {
            _telegramHub = telegramHub;
            _telegramMonitorInstances = new Dictionary<Guid,TelegramMonitorInstance>();
        }

        public IList<TelegramMonitorInstance> GetMonitorInstances()
        {
            return _telegramMonitorInstances.Values.ToList();
        }

        private ITelegramMonitorInstance CreateTelegramMonitor(TelegramMonitorInstance instance)
        {
            _telegramMonitorInstances[instance.Id] = instance;
            return new TelegramMonitorImpl(instance.Id, this);
        }

        public async Task NotifyTelegram(ITelegramMessage message)
        {
            await _telegramHub.Clients.All.SendAsync("OnTelegram", message);
        }

        public ITelegramMonitorInstance CreateTelegramMonitor(Guid id, string name, string busType, string description)
        {
            return CreateTelegramMonitor(new TelegramMonitorInstance(id, name, busType, description));
        }

        public ITelegramMonitorInstance CreateTelegramMonitor(NodeInstance instance, string busType)
        {
            return CreateTelegramMonitor(instance.ObjId, instance.Name, busType, instance.Description);
        }

        public void Clear()
        {
            _telegramMonitorInstances.Clear();
        }
    }
}
