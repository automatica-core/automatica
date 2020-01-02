using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Extensions;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.IkeaTradfriDriverFactory.Devices.Light;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory.Devices
{
    public class IkeaTradfriContainerNode : DriverBase
    {
        internal IIkeaTradfriGateway Gateway { get; }
        public long DeviceId { get; private set; }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public IkeaTradfriContainerNode(IDriverContext driverContext, IIkeaTradfriGateway gateway) : base(driverContext)
        {
            Gateway = gateway;
        }

        public async Task Reconnect()
        {

            var cancellation = new CancellationTokenSource(10000);
            if (!await _semaphore.WaitAsync(10))
            {
                return;
            }
            DriverContext.Logger.LogInformation($"Reconnect to tradfri gateway!");
            
            await Gateway.Stop();
            try
            {
                await Gateway.Start().WithCancellation(cancellation.Token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogInformation($"Could not reconnect to gateway...{e}");
            }
            finally
            {
                _semaphore.Release(1);
            }
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
                return tradfriDevice;
            }
            return null;
        }
    }
}
