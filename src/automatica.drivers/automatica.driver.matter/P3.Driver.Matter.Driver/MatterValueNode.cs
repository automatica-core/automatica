using Automatica.Core.Driver;

namespace P3.Driver.Matter.Driver
{
    /// <summary>A read-only value node (pairing code, QR payload, pairing state) fed by the driver.</summary>
    internal sealed class MatterValueNode : DriverNoneAttributeBase
    {
        private object _value;

        public MatterValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void Publish(object value)
        {
            if (Equals(_value, value))
            {
                return;
            }

            _value = value;
            DispatchRead(value);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            if (_value != null)
            {
                await readContext.DispatchValue(_value, token);
            }

            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
