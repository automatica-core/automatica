using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriRelayNode : IkeaTradfriDevice
    {
        private bool _value;

        public IkeaTradfriRelayNode(IDriverContext driverContext, IkeaTradfriContainerNode container, TradfriDeviceType deviceType) : base(driverContext, container, deviceType)
        {
        }
        
        public override Task<bool> Read()
        {
            DispatchValue(_value);

            return Task.FromResult(true);
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            return Task.Run(() =>
            {
                var bValue = Convert.ToBoolean(value);

                _value = bValue;

                DriverContext.Logger.LogDebug($"Start write {DriverContext.NodeInstance.Name}");
                if (bValue)
                {
                    Container.Gateway.Driver.SwitchOn(Container.DeviceId);
                }
                else
                {
                    Container.Gateway.Driver.SwitchOff(Container.DeviceId);
                }
                DriverContext.Logger.LogDebug($"Done write {DriverContext.NodeInstance.Name}");

                return base.WriteValue(source, value);
            });
        }

        protected override void Update(JToken device)
        {
            if (device is JArray array)
            {
                var valueProp = ((int) TradfriConstAttribute.LightState).ToString();

                var boolValue = Convert.ToBoolean(array.First()[valueProp]);

                if (boolValue != _value)
                {
                    _value = boolValue;
                    DispatchValue(_value);
                }
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
