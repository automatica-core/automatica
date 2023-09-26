using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Driver
{
    internal class WriteContext : IWriteContext
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDispatchable _dispatchable;

        public WriteContext(IDispatcher dispatcher, IDispatchable dispatchable, bool writeOnlyIfChanged=true)
        {
            _dispatcher = dispatcher;
            _dispatchable = dispatchable;

            WriteOnlyIfChanged = writeOnlyIfChanged;
        }

        public bool WriteOnlyIfChanged { get; }

        public Task DispatchValue(object value, CancellationToken token = default)
        {
            return _dispatcher.DispatchValue(_dispatchable, value, DispatchValueSource.Write);
        }
    }
}
