using System;
using Automatica.Core.Driver;
using P3.Driver.IkeaTradfri.Models;
using P3.Driver.IkeaTradfriDriverFactory.Devices.Light;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriContainerNode : DriverBase
    {
        internal IkeaTradfriGateway Gateway { get; }
        public long DeviceId { get; private set; }

        public IkeaTradfriContainerNode(IDriverContext driverContext, IkeaTradfriGateway gateway) : base(driverContext)
        {
            Gateway = gateway;
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
