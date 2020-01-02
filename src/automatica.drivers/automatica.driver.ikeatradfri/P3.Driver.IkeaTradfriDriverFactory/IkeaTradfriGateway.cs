using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.IkeaTradfri;
using P3.Driver.IkeaTradfri.Models;
using P3.Driver.IkeaTradfriDriverFactory.Devices;

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public class IkeaTradfriGateway : DriverBase, IIkeaTradfriGateway
    {
        private string _id;
        private string _secret;
        private string _appKey;


        public IIkeaTradfriDriver Driver { get; set; }
        public List<IkeaTradfriDevice> Devices { get; } = new List<IkeaTradfriDevice>();

        public IkeaTradfriGateway(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override bool Init()
        {
            _id = DriverContext.NodeInstance.GetPropertyValueString(IkeaTradfriFactory.IdAddressPropertyKey);
            _secret = DriverContext.NodeInstance.GetPropertyValueString(IkeaTradfriFactory.SecretPropertyKey);

            return base.Init();
        }

        public override Task<IList<NodeInstance>> Scan()
        {
            var devices = Driver.LoadDevices();
            IList<NodeInstance> ret = new List<NodeInstance>();

            foreach (var device in devices)
            {
                if (Devices.Any(a => a.Container.DeviceId == device.Id))
                {
                    continue;
                }

                NodeInstance node = null;
                switch (device.ApplicationType)
                {
                    case DeviceType.Remote:
                        break;
                    case DeviceType.Unknown:
                        break;
                    case DeviceType.Light:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightContainerGuid);

                        var lightNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightGuid);
                        node.InverseThis2ParentNodeInstanceNavigation.Add(lightNode);

                        var lighColorNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightColorGuid);
                        node.InverseThis2ParentNodeInstanceNavigation.Add(lighColorNode);

                        var lightDimmerNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.LightDimmerGuid);
                        node.InverseThis2ParentNodeInstanceNavigation.Add(lightDimmerNode);
                        break;
                    case DeviceType.PowerOutlet:
                        node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.RelayContainerGuid);

                        var stateNode = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.RelayGuid);
                        node.InverseThis2ParentNodeInstanceNavigation.Add(stateNode);
                        break;
                }

                if (node != null)
                {
                    var idProp = node.GetProperty(IkeaTradfriFactory.DeviceIdPropertyKey);
                    idProp.Value = device.Id;
                    node.Name = device.Name;

                    ret.Add(node);
                }
            }

            return Task.FromResult(ret);
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

            await Task.Run(() =>
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
                Driver.Connect();
            });

           return await base.Start();
        }
        

        public override Task<bool> Stop()
        {
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new IkeaTradfriContainerNode(ctx, this);
        }
    }
}
