using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    internal class PairingKeyNode : DriverNoneAttributeBase
    {
        public PairingKeyNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public string Code { get; internal set; }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(Code, token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
