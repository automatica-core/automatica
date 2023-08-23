using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;


namespace P3.Driver.SonosDriverFactory
{
    public class SonosAttribute : DriverBase
    {
        private readonly Func<Task<object>> _readAction;
        private readonly Func<object, Task<object>> _writeAction;

        private object? _lastValue = null;

        public SonosAttribute(IDriverContext ctx, Func<Task<object>> readAction, Func<object, Task<object>> writeAction) : base(ctx)
        {
            _readAction = readAction;
            _writeAction = writeAction;

        }

        public override async Task<bool> Read(CancellationToken token = new CancellationToken())
        {
            if (_readAction != null)
            {
                try
                {
                    var value = await _readAction.Invoke();

                    if (value != null && value != _lastValue)
                    {
                        _lastValue = value;
                        DispatchValue(value);
                    }
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Could not read value...");
                }
            }

            return true;
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            try
            {
                var write = await _writeAction.Invoke(value);
                DriverContext.Logger.LogDebug($"Sonos write value {write}...");

                if (write != null && write != _lastValue)
                {
                    _lastValue = write;
                    DispatchValue(write);
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error write value to sonos...");
            }
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            Read(token).ConfigureAwait(false);
            return base.Start(token);
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null; //we have no more children, therefore return null
        }
    }
}
