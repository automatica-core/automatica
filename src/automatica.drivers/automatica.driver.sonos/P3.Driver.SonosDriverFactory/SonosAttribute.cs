using System;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;


namespace P3.Driver.SonosDriverFactory
{
    public class SonosAttribute : DriverBase
    {
        private readonly SonosDevice _device;
        private readonly Func<Task<object>> _readAction;
        private readonly Action<object> _writeAction;
        private readonly Timer _readTimer = new Timer();



        public SonosAttribute(IDriverContext ctx, SonosDevice device, Func<Task<object>> readAction, Action<object> writeAction) : base(ctx)
        {
            _device = device;
            _readAction = readAction;
            _writeAction = writeAction;

            _readTimer.Elapsed += ReadTimerOnElapsed;
            _readTimer.Interval = TimeSpan.FromSeconds(2).TotalMilliseconds;
        }

        private async void ReadTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await ReadValue();
        }

        protected virtual async Task ReadValue()
        {
            if (_readAction != null)
            {
                try
                {
                    var value = await _readAction.Invoke();

                    if (value != null)
                    {
                        DispatchValue(value);
                    }
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Could not read value...");
                }
            }
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            try
            {
                _writeAction.Invoke(value);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error write value to sonos...");
            }

            return Task.CompletedTask;
        }

        public override async Task<bool> Start()
        {
           await ReadValue();

            _readTimer.Start();

            return await base.Start();
        }

        public override Task<bool> Stop()
        {
            _readTimer.Elapsed += ReadTimerOnElapsed;
            _readTimer.Stop();
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null; //we have no more children, therefore return null
        }
    }
}
