using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightDimmerNode : IkeaTradfriDevice
    {
        private int _value;
        public IkeaTradfriLightDimmerNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, TradfriDeviceType.LightControl)
        {
        }

        protected override void Update(JToken device)
        {
            if (device is JArray array)
            {
                var valueProp = ((int)TradfriConstAttribute.LightDimmer).ToString();

                var value = Convert.ToInt32(array.First()[valueProp]);

                if (value != _value)
                {
                    _value = value;
                    DispatchValue(_value);
                }
            }
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
