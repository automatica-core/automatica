using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightColorNode : IkeaTradfriDevice
    {
        private string _value;

        public IkeaTradfriLightColorNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, DeviceType.Light)
        {
        }

        protected override void Update(TradfriDevice device)
        {
            _value = device.LightControl[0].ColorHex;
            DispatchValue(_value);
        }

        public override Task<bool> Read()
        {
            DispatchValue(_value);

            return Task.FromResult(true);
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var strValue = Convert.ToString(value);

            _value = strValue;

            Container.Gateway.Driver.SetColor(Container.DeviceId, strValue);
            return base.WriteValue(source, value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
