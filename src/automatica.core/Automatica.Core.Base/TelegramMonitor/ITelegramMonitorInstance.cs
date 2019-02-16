using System.Threading.Tasks;

namespace Automatica.Core.Base.TelegramMonitor
{
    /// <summary>
    /// Interace to send telegrams to the UI for analyse purpose
    /// </summary>
    public interface ITelegramMonitorInstance
    {
        Task NotifyTelegram(TelegramDirection direction, string sourceAddress, string targetAddress, string data, string additionalMessage);
    }
}
