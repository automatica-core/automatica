using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Extensions;
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

        public bool Value => _value;

        public int WriteTimeout { get; internal set; } = 5000;

        public IkeaTradfriRelayNode(IDriverContext driverContext, IkeaTradfriContainerNode container, TradfriDeviceType deviceType) : base(driverContext, container, deviceType)
        {
        }
        
        public override Task<bool> Read()
        {
            DispatchValue(_value);

            return Task.FromResult(true);
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            var cancellation = new CancellationTokenSource(WriteTimeout);
            try
            {
                await Task.Run(async () =>
                {
                    var bValue = Convert.ToBoolean(value);

                    _value = bValue;

                    DriverContext.Logger.LogDebug($"Start write {bValue} to {DriverContext.NodeInstance.Name}...");
                    if (bValue)
                    {
                        await Container.Gateway.Driver.SwitchOn(Container.DeviceId);
                    }
                    else
                    {
                        await Container.Gateway.Driver.SwitchOff(Container.DeviceId);
                    }

                    DriverContext.Logger.LogDebug($"Done write {DriverContext.NodeInstance.Name}");

                }, cancellation.Token).WithCancellation(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                await Container.Reconnect();
            }
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
