using System;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var write = await _writeAction.Invoke(value);
                DriverContext.Logger.LogDebug($"Sonos write value {write}...");
              //  await writeContext.DispatchValue(write, token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error write value to sonos...");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            if (_readAction != null)
            {
                try
                {
                    var value = await _readAction.Invoke();

                    if (value != null && !value.Equals(_lastValue))
                    {
                        _lastValue = value;
                        await readContext.DispatchValue(value, token);
                    }
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Could not read value...");
                }
            }

            return true;
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
