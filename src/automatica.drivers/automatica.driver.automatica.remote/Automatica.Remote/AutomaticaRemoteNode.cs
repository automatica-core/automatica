using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using System.Threading.Tasks;

namespace P3.Driver.Automatica.Remote
{
    public class AutomaticaRemoteNode : DriverBase
    {
        private readonly AutomaticaRemoteDriver _driver;

        public AutomaticaRemoteNode(IDriverContext driverContext, AutomaticaRemoteDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            _driver.WriteValue(this, value);
            return base.WriteValue(source, value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = new AutomaticaRemoteNode(ctx, _driver);

            _driver.AddRemoteNode(node, ctx.NodeInstance.ObjId);

            return node;
        }
    }
}
