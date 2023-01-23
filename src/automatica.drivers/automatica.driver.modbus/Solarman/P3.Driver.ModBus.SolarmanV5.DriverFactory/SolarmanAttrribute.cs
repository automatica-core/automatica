using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal abstract class SolarmanAttrribute : DriverBase
    {
        private readonly SolarmanGroupAttribute _parent;
        public int Offset { get; set; }
        public double  Scale { get; set; }
        public bool IsSigned { get; set; }

        protected SolarmanAttrribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext)
        {
            _parent = parent;
        }

        public override bool Init()
        {
            Offset = GetPropertyValueInt("offset");
            Scale = GetPropertyValueDouble("scale");
            IsSigned = GetProperty("signed").ValueBool.Value;
            return base.Init();
        }

        public override Task<bool> Read()
        {
            _parent.PollAttributes().ConfigureAwait(false);
            return Task.FromResult(true);
        }

        public abstract Task<object> ConvertValue(ModBusRegisterValueReturn modbusReturn);

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
