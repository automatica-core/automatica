using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.IkeaTradfri;

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public class IkeaTradfri : DriverNoneAttributeBase
    {
        private readonly Dictionary<string, NodeInstance> _existingDevices = new Dictionary<string, NodeInstance>();

        public IkeaTradfri(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var ret = new List<NodeInstance>();
            var gateways = await IkeaTradfriDriver.Discover();

            foreach (var gw in gateways)
            {
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.GatewayGuid);
                node.Name = gw.Item1;

                if (_existingDevices.ContainsKey(gw.Item1))
                {
                    continue;
                }

                var id = node.GetProperty(IkeaTradfriFactory.IdAddressPropertyKey);
                id.Value = gw.Item1;

                ret.Add(node);
            }

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var id = ctx.NodeInstance.GetPropertyValueString(IkeaTradfriFactory.IdAddressPropertyKey);
            
            if (_existingDevices.ContainsKey(id))
            {
                DriverContext.Logger.LogError($"Tradfri gateway with id {id} already created");
                return null;
            }

            _existingDevices.Add(id, ctx.NodeInstance);

            return new IkeaTradfriGateway(ctx);
        }
    }
}
