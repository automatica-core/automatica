using Automatica.Core.Driver;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightNode : IkeaTradfriRelayNode
    {
        public IkeaTradfriLightNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, TradfriDeviceType.LightControl)
        {
        }
    }
}
