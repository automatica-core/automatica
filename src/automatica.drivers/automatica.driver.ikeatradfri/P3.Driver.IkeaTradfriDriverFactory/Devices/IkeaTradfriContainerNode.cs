using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.IkeaTradfri.Models;
using P3.Driver.IkeaTradfriDriverFactory.Devices.Light;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriContainerNode : DriverBase
    {
        internal IIkeaTradfriGateway Gateway { get; }
        public long DeviceId { get; private set; }

        public IkeaTradfriContainerNode(IDriverContext driverContext, IIkeaTradfriGateway gateway) : base(driverContext)
        {
            Gateway = gateway;
        }

        public async Task Reconnect()
        {
            DriverContext.Logger.LogInformation($"Reconnect to tradfri gateway!");
            await Gateway.Stop();
            await Gateway.Start();
        }

        public override bool Init()
        {
            DeviceId = Convert.ToInt64(DriverContext.NodeInstance.GetPropertyValueDouble(IkeaTradfriFactory.DeviceIdPropertyKey));

            return base.Init();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            IkeaTradfriDevice tradfriDevice = null;
            if (ctx.NodeInstance.This2NodeTemplate == IkeaTradfriFactory.RelayGuid)
            {
                tradfriDevice = new IkeaTradfriRelayNode(ctx, this, TradfriDeviceType.SwitchPlug);
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
                return tradfriDevice;
            }
            return null;
        }
    }
}
