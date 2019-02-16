using System.Threading.Tasks;

namespace Automatica.Core.Base.TelegramMonitor
{
    /// <summary>
    /// Default implementation of <see cref="ITelegramMonitorInstance"/>
    /// </summary>
    public class EmptyTelegramMonitorInstance : ITelegramMonitorInstance
    {
        public Task NotifyTelegram(TelegramDirection direction, string sourceAddress, string targetAddress, string data, string additionalMessage)
        {
            return Task.CompletedTask;
        }
    }
}
