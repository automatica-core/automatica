using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos.Discovery;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDriver : DriverBase
    {
        private readonly Dictionary<string, NodeInstance> _existingDevices = new Dictionary<string, NodeInstance>();

        public SonosDriver(IDriverContext ctx) : base(ctx)
        {
        }


        public override async Task<IList<NodeInstance>> Scan()
        {
            var ret = new List<NodeInstance>();
            var devices = await SonosDiscovery.DiscoverSonos();

            foreach (var device in devices)
            {
                if(_existingDevices.ContainsKey(device.Uuid))
                {
                    continue;
                }

                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(SonosDriverFactory.SonosDeviceGuid);
                node.Name = device.Uuid;

                var id = node.GetProperty(SonosDriverFactory.IdAddressPropertyKey);
                id.Value = device.Uuid;

                var ip = node.GetProperty(SonosDriverFactory.IpAddressPropertyKey);
                ip.Value = device.Location.Host;

                ret.Add(node);
            }

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var id = ctx.NodeInstance.GetPropertyValueString(SonosDriverFactory.IdAddressPropertyKey);

            if (_existingDevices.ContainsKey(id))
            {
                DriverContext.Logger.LogError($"Sonos device with id {id} already created");
                return null;
            }

            _existingDevices.Add(id, ctx.NodeInstance);

            return new SonosDevice(ctx);
        }
    }
}
