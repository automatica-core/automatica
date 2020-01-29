using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteTelegramMonitorInstance : ITelegramMonitorInstance
    {
        public Task NotifyTelegram(TelegramDirection direction, string sourceAddress, string targetAddress, string data,
            string additionalMessage)
        {
            return Task.CompletedTask;
        }
    }
}
