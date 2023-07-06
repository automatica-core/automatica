using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

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

        public override Task<bool> Init(CancellationToken token = default)
        {
            Offset = GetPropertyValueInt("offset");
            Scale = GetPropertyValueDouble("scale");
            IsSigned = GetProperty("signed").ValueBool.Value;
            return base.Init(token);
        }

        public override Task<bool> Read(CancellationToken token = default)
        {
            _parent.Read().ConfigureAwait(false);
            return Task.FromResult(true);
        }

        public abstract Task<object> ConvertValue(ushort[] data);

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
