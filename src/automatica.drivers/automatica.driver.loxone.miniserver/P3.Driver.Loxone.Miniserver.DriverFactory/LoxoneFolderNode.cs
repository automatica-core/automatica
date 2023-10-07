using Automatica.Core.Driver;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneFolderNode : DriverNoneAttributeBase
    {
        private readonly LoxoneDriver _driver;

        public LoxoneFolderNode(IDriverContext driverContext, LoxoneDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new LoxoneDriverNode(ctx, _driver);
        }
    }
}
