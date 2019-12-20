using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
            var cancellation = new CancellationTokenSource(5000);
            return Task.Run(async () =>
            {
                try
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

                }
                catch (TaskCanceledException)
                {
                    await Container.Reconnect();
                }

                return base.WriteValue(source, value);
            }, cancellation.Token);
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
