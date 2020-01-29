using Automatica.Core.Driver;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightNode : IkeaTradfriRelayNode
    {
        public IkeaTradfriLightNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, DeviceType.Light)
        {
        }
    }
}
