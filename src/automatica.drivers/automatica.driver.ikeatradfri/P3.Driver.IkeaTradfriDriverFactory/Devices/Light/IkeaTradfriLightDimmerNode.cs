using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightDimmerNode : IkeaTradfriAttribute
    {
        private long _value;
        public IkeaTradfriLightDimmerNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, DeviceType.Light)
        {
        }

        internal override void Update(TradfriDevice device)
        {
            _value = device.LightControl[0].Dimmer;
            DispatchRead(_value);
        }
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var intValue = Convert.ToInt32(value);

            _value = intValue;

            await Container.Gateway.Driver.SetDimmer(Container.DeviceId, intValue);
            await writeContext.DispatchValue(intValue, token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
