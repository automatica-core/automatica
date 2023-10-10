using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Driver.ShellyFactory.Discovery;
using Automatica.Driver.ShellyFactory.Types;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.ShellyFactory
{
    internal class ShellyDriver : DriverBase
    {
        public List<ShellyDevice> DiscoveredShellys { get; private set; } = new List<ShellyDevice>();
        public ShellyDiscoveryService DiscoveryService { get; }
        private readonly List<ShellyDriverDevice> _devices = new List<ShellyDriverDevice>();
        public ShellyDriver(IDriverContext driverContext) : base(driverContext)
        {
            DiscoveryService = new ShellyDiscoveryService(driverContext.ZeroconfDiscovery, driverContext.Logger);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override async Task<bool> Init(CancellationToken token = new CancellationToken())
        {
            DiscoveredShellys = await DiscoveryService.SearchShellys();

            return await base.Init(token);
        }


        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = new CancellationToken())
        {
            var devices = await DiscoveryService.SearchShellys();
            var ret = new List<NodeInstance>();

            foreach (var device in devices)
            {
                if (_devices.Any(a => a.ShellyId == device.Id))
                {
                    DriverContext.Logger.LogInformation($"Device with id {device.Id} already added...");
                    continue;
                }

                NodeInstance node;
                switch (device.Type)
                {
                    case ShellyDeviceType.Shelly1Pm:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(ShellyFactory.Shelly1Device);
                        break;
                    case ShellyDeviceType.Shelly25:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(ShellyFactory.Shelly25Device);
                        break;
                    default:
                        DriverContext.Logger.LogInformation($"Device with id {device.Id} not supported...");
                        continue;
                
                }
                var idProp = node.GetProperty(ShellyFactory.DeviceIdPropertyKey);
                idProp.Value = device.Id;
                node.Name = device.Name;

                ret.Add(node);
            }


            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;

            ShellyDriverDevice shellyDevice;
            switch (key)
            {
                case "shelly-1":
                    shellyDevice = new Shelly1Device(ctx);
                    break;
                case "shelly-25":
                    shellyDevice = new Shelly25Device(ctx);
                    break;
                default:
                    DriverContext.Logger.LogWarning($"Could not found implementation type for {key} on {ctx.NodeInstance.Name}");
                    return null;
            }

            _devices.Add(shellyDevice);
            return shellyDevice;
        }
    }
}
