using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using System.Threading;
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

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            // the remote server echoes the new value back through the subscription
            return _driver.SendValue(this, value, token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = new AutomaticaRemoteNode(ctx, _driver);

            _driver.AddRemoteNode(node, ctx.NodeInstance.ObjId);

            return node;
        }
    }
}
