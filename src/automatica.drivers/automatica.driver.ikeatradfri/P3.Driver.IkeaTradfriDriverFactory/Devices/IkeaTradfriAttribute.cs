using Automatica.Core.Driver;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public abstract class IkeaTradfriAttribute : DriverBase
    {
        internal DeviceType DeviceType { get; }
        internal IkeaTradfriContainerNode Container { get; }
        
        protected IkeaTradfriAttribute(IDriverContext driverContext, IkeaTradfriContainerNode container, DeviceType deviceType) : base(driverContext)
        {
            DeviceType = deviceType;
            Container = container;
        }

        internal abstract void Update(TradfriDevice device);

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
