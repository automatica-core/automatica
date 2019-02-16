using Automatica.Core.Base.TelegramMonitor;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.Driver.Monitor
{
    /// <summary>
    /// Implements the <see cref="ITelegramMonitorInstance"/>
    /// </summary>
    public class TelegramMonitorImpl : ITelegramMonitorInstance
    {
        private readonly Guid _busId;
        private readonly ITelegramMonitor _monitor;

        public TelegramMonitorImpl(Guid id, ITelegramMonitor monitor)
        {
            _busId = id;
            _monitor = monitor;
        }
        private Task NotifyTelegram(ITelegramMessage message)
        {
            return _monitor.NotifyTelegram(message);
        }

        public Task NotifyTelegram(TelegramDirection direction, string sourceAddress, string targetAddress, string data, string additionalMessage)
        {
            return NotifyTelegram(new TelegramMessage(_busId, direction, sourceAddress, targetAddress, data, additionalMessage));
        }
    }
}
