using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Extensions;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.IkeaTradfriDriverFactory.Devices.Light;
using Tomidix.NetStandard.Tradfri.Models;
using Exception = System.Exception;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriContainerNode : DriverBase
    {
        internal IIkeaTradfriGateway Gateway { get; }
        public long DeviceId { get; private set; }

        private readonly IList<IkeaTradfriAttribute> _devices = new List<IkeaTradfriAttribute>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public IkeaTradfriContainerNode(IDriverContext driverContext, IIkeaTradfriGateway gateway) : base(driverContext)
        {
            Gateway = gateway;
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            await base.Start(token);

            try
            {

                await Gateway.Driver.RegisterChange(a =>
                {
                    try
                    {
                        if (a == null)
                        {
                            return;
                        }

                        Update(a);

                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError(e, "Could not update tradfri device state");
                    }

                }, DeviceId);

                var device = await Gateway.Driver.GetDevice(DeviceId);
                Update(device);
                DriverContext.Logger.LogDebug($"{FullName}: {JsonConvert.SerializeObject(device)}");
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error starting tradfri container...");
            }

            return true;
        }

        private void Update(TradfriDevice device)
        {
            foreach (var att in _devices)
            {
                att.Update(device);
            }
        }
        public override Task<bool> Init(CancellationToken token = default)
        {
            DeviceId = Convert.ToInt64(DriverContext.NodeInstance.GetPropertyValueDouble(IkeaTradfriFactory.DeviceIdPropertyKey));

            return base.Init(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            IkeaTradfriAttribute tradfriDevice = null;
            if (ctx.NodeInstance.This2NodeTemplate == IkeaTradfriFactory.RelayGuid)
            {
                tradfriDevice = new IkeaTradfriRelayNode(ctx, this, DeviceType.ControlOutlet);
            }
            else if (ctx.NodeInstance.This2NodeTemplate == IkeaTradfriFactory.LightGuid)
            {
                tradfriDevice = new IkeaTradfriLightNode(ctx, this);
            }
            else if (ctx.NodeInstance.This2NodeTemplate == IkeaTradfriFactory.LightColorGuid)
            {
                tradfriDevice = new IkeaTradfriLightColorNode(ctx, this);
            }
            else if (ctx.NodeInstance.This2NodeTemplate == IkeaTradfriFactory.LightDimmerGuid)
            {
                tradfriDevice = new IkeaTradfriLightDimmerNode(ctx, this);
            }

            if (tradfriDevice != null)
            {
                Gateway.Devices.Add(tradfriDevice);
                _devices.Add(tradfriDevice);
                return tradfriDevice;
            }
            return null;
        }
    }
}
