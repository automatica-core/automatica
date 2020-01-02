using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Newtonsoft.Json.Linq;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightDimmerNode : IkeaTradfriDevice
    {
        private long _value;
        public IkeaTradfriLightDimmerNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, DeviceType.Light)
        {
        }

        protected override void Update(TradfriDevice device)
        {
            _value = device.LightControl[0].Dimmer;
            DispatchValue(_value);
        }
        public override Task<bool> Read()
        {
            DispatchValue(_value);

            return Task.FromResult(true);
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var intValue = Convert.ToInt32(value);

            _value = intValue;

            Container.Gateway.Driver.SetDimmer(Container.DeviceId, intValue);
            return base.WriteValue(source, intValue);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
