using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Extensions;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriRelayNode : IkeaTradfriDevice
    {
        private bool _value;

        public bool Value => _value;

        public bool LastWriteState { get; set; }

        public int WriteTimeout { get; internal set; } = 5000;

        public IkeaTradfriRelayNode(IDriverContext driverContext, IkeaTradfriContainerNode container, DeviceType deviceType) : base(driverContext, container, deviceType)
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
                        if (!await Container.Gateway.Driver.SwitchOn(Container.DeviceId))
                        {
                            throw new InvalidOperationException("Return code not successful");
                        }
                    }
                    else
                    {
                        if (!await Container.Gateway.Driver.SwitchOff(Container.DeviceId))
                        {
                            throw new InvalidOperationException("Return code not successful");
                        }
                    }

                    DriverContext.Logger.LogDebug($"Done write {DriverContext.NodeInstance.Name}");
                    LastWriteState = true;

                }, cancellation.Token).WithCancellation(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                LastWriteState = false;
                
                DriverContext.Logger.LogDebug($"Operation cancelled, will try to reconnect");
                await Container.Reconnect();
            }
            catch (InvalidOperationException)
            {
                LastWriteState = false;

                DriverContext.Logger.LogDebug($"Lost connection, will try to reconnect");
                await Container.Reconnect();

            }
        }

        protected override void Update(TradfriDevice device)
        {
            _value = device.Control[0].State == Bool.True;
            DispatchValue(_value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
