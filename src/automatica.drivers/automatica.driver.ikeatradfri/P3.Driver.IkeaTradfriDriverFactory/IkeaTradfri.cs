using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.IkeaTradfri;

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public class IkeaTradfri : DriverBase
    {
        public IkeaTradfri(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<IList<NodeInstance>> Scan()
        {
            var ret = new List<NodeInstance>();
            var gateways = await IkeaTradfriDriver.Discover();

            foreach (var gw in gateways)
            {
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(IkeaTradfriFactory.GatewayGuid);
                node.Name = gw.Item1;

                var id = node.GetProperty(IkeaTradfriFactory.IdAddressPropertyKey);
                id.Value = gw.Item1;

                ret.Add(node);
            }

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new IkeaTradfriGateway(ctx);
        }
    }
}
