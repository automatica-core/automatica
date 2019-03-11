using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Factories.Baos
{
    public class KnxBaosDatapoints : DriverBase
    {
        private readonly KnxBaosDriver _baosDriver;

        public KnxBaosDatapoints(IDriverContext driverContext, KnxBaosDriver baosDriver) : base(driverContext)
        {
            _baosDriver = baosDriver;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = KnxMiddleGroup.CreateDriverNode(ctx.NodeInstance.This2NodeTemplateNavigation.Key, ctx, _baosDriver);
            return node;
        }
    }
}
