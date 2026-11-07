using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.NetworkPresence.Driver
{
    /// <summary>
    /// A read-only data point that only dispatches when its value changes
    /// </summary>
    internal sealed class ValueNode : DriverNotWriteableBase
    {
        private readonly object _lock = new();
        private bool _hasValue;

        public object LastValue { get; private set; }

        public ValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void Set(object value)
        {
            lock (_lock)
            {
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }
                _hasValue = true;
                LastValue = value;
            }
            DispatchRead(value);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            if (_hasValue)
            {
                DispatchRead(LastValue);
            }
            return Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
