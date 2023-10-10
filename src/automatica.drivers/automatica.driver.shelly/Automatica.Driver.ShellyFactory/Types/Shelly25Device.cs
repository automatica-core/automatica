using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal class Shelly25Device : Shelly1Device
    {
        public Shelly25Device(IDriverContext driverContext, ITelegramMonitorInstance telegramMonitorInstance) : base(driverContext, telegramMonitorInstance)
        {
        }
    }
}
