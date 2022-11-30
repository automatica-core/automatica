using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal abstract class SolarmanAttrribute : DriverBase
    {
        public int Offset { get; set; }
        public double  Scale { get; set; }
        public bool IsSigned { get; set; }

        protected SolarmanAttrribute(IDriverContext driverContext, SolarmanDriver parent) : base(driverContext)
        {
        }

        public override bool Init()
        {
            Offset = GetPropertyValueInt("offset");
            Scale = GetPropertyValueDouble("scale");
            IsSigned = GetProperty("signed").ValueBool.Value;
            return base.Init();
        }

        public abstract Task<object> ConvertValue(ModBusRegisterValueReturn modbusReturn);

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
