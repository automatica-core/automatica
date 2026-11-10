using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal abstract class SolarmanAttrribute : DriverNotWriteableBase
    {
        private readonly SolarmanGroupAttribute _parent;
        public int Offset { get; set; }
        public double  Scale { get; set; }
        public bool IsSigned { get; set; }

        /// <summary>
        /// Set by the group when the register map marks the register as signed. The "signed" property of nodes
        /// created before the register map knew about it is false and cannot be edited, so the map wins.
        /// </summary>
        public bool SignedInRegisterMap { get; set; }

        protected SolarmanGroupAttribute Group => _parent;

        protected SolarmanAttrribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext)
        {
            _parent = parent;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            Offset = GetPropertyValueInt("offset");
            Scale = GetPropertyValueDouble("scale");
            IsSigned = SignedInRegisterMap || (GetProperty("signed")?.ValueBool ?? false);
            return base.Init(token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            _parent.Read(token).ConfigureAwait(false);
            return Task.FromResult(true);
        }

        public abstract Task<object> ConvertValue(ushort[] data);

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
