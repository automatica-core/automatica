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
    public class IkeaTradfriRelayNode : IkeaTradfriAttribute
    {
        private bool _value;

        public bool Value => _value;

        public bool LastWriteState { get; set; }

        public int WriteTimeout { get; internal set; } = 5000;

        public IkeaTradfriRelayNode(IDriverContext driverContext, IkeaTradfriContainerNode container, DeviceType deviceType) : base(driverContext, container, deviceType)
        {
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
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
                    await writeContext.DispatchValue(bValue, cancellation.Token);

                }, cancellation.Token).WithCancellation(cancellation.Token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error write state");
                LastWriteState = false;
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }


        internal override void Update(TradfriDevice device)
        {
            if (DeviceType == DeviceType.ControlOutlet)
            {
                if (device?.Control == null || device.Control.Count == 0)
                {
                    DriverContext.Logger.LogDebug($"Invalid device received, no control outlet found");
                    return;
                }

                _value = device.Control[0].State == Bool.True;
                DispatchRead(_value);
            }
            else if(DeviceType == DeviceType.Light)
            {
                if (device?.LightControl == null || device.LightControl.Count == 0)
                {
                    DriverContext.Logger.LogDebug($"Invalid device received, no control outlet found");
                    return;
                }

                _value = device.LightControl[0].State == Bool.True;
                DispatchRead(_value);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
