using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.IkeaTradfri;
using P3.Driver.IkeaTradfriDriverFactory.Devices;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public class IkeaTradfriGateway : DriverBase, IIkeaTradfriGateway
    {
        private string _id;
        private string _secret;
        private string _appKey;


        public IIkeaTradfriDriver Driver { get; set; }
        public List<IkeaTradfriAttribute> Devices { get; } = new List<IkeaTradfriAttribute>();

        public IkeaTradfriGateway(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override bool Init()
        {
            _id = DriverContext.NodeInstance.GetPropertyValueString(IkeaTradfriFactory.IdAddressPropertyKey);
            _secret = DriverContext.NodeInstance.GetPropertyValueString(IkeaTradfriFactory.SecretPropertyKey);

            return base.Init();
        }

        public override async Task<IList<NodeInstance>> Scan()
        {
            var devices = await Driver.LoadDevices();
            IList<NodeInstance> ret = new List<NodeInstance>();

            DriverContext.Logger.LogDebug($"Found {devices.Count} devices");
            foreach (var device in devices)
            {
                DriverContext.Logger.LogDebug($"Working on device {device.ID}, {device.Name} with Type {device.DeviceType}");
                if (Devices.Any(a => a.Container.DeviceId == device.ID))
                {
                    continue;
                }

                NodeInstance node = null;
                switch (device.DeviceType)
                {
                    case DeviceType.Remote:
                        break;
                    case DeviceType.Light:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightContainerGuid);

                        var lightNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightGuid);
                        node.InverseThis2ParentNodeInstanceNavigation.Add(lightNode);

                        break;
                    case DeviceType.ControlOutlet:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.RelayContainerGuid);
                        break;
                }

                if (node != null)
                {
                    var idProp = node.GetProperty(IkeaTradfriFactory.DeviceIdPropertyKey);
                    idProp.Value = device.ID;
                    node.Name = device.Name;

                    ret.Add(node);
                }
            }

            return ret;
        }


        public override async Task<bool> Start()
        {
            if (DriverContext.IsTest)
            {
                return true;
            }

            if (String.IsNullOrEmpty(_id))
            {
                DriverContext.Logger.LogInformation("Could not start driver, the gateway id is invalid");
                return false;
            }

            var scan = await IkeaTradfriDriver.Discover();
            var gw = scan.Single(a => a.Item1 == _id.ToLowerInvariant());

            if (gw == null)
            {
                DriverContext.Logger.LogInformation($"Could not find gateway with id {_id}");
                return false;
            }
            DriverContext.Logger.LogInformation($"Connecting to tradfri with ip {gw.Item2}");

            await Task.Run(async () =>
            {
                var conName = $"Automatica.Core-{DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}";
                if (String.IsNullOrEmpty(_appKey))
                {
                    var auth = IkeaTradfriDriver.GeneratePsk(gw.Item2, conName, _secret);
                    if (auth == null)
                    {
                        DriverContext.Logger.LogError($"Could not generate psk key for tradfri {Name}");
                        return;
                    }
                    _appKey = auth.PSK;

                    var prop = DriverContext.NodeInstance.GetProperty(IkeaTradfriFactory.ConnectionPropertyKey);

                    DriverContext.NodeTemplateFactory.SetPropertyValue(prop.ObjId, _appKey);
                }

                Driver = new IkeaTradfriDriver(gw.Item2, conName, _appKey);
                await Driver.Connect();
            });

           return await base.Start();
        }
        

        public override async Task<bool> Stop()
        {
            if (Driver != null)
            {
                await Driver.Disconnect();
            }
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new IkeaTradfriContainerNode(ctx, this);
        }
    }
}
