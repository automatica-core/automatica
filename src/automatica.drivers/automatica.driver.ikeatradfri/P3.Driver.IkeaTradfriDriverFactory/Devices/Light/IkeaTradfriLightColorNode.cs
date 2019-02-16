using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices.Light
{
    public class IkeaTradfriLightColorNode : IkeaTradfriDevice
    {
        private string _value;

        public IkeaTradfriLightColorNode(IDriverContext driverContext, IkeaTradfriContainerNode container) : base(driverContext, container, TradfriDeviceType.LightControl)
        {
        }

        protected override void Update(JToken device)
        {
            if (device is JArray array)
            {
                var valueProp = ((int)TradfriConstAttribute.LightColorHex).ToString();

                var strValue = array.First()[valueProp].ToString();

                if (strValue != _value)
                {
                    _value = strValue;
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
