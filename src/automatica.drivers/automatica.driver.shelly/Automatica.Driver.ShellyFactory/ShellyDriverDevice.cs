using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Automatica.Driver.ShellyFactory
{
    internal abstract class ShellyDriverDevice : DriverBase
    {
        private Timer _timer;
        public string ShellyId { get; }
        public int PollingInterval { get; }
        public int PollFailCount { get; private set; }

        protected ShellyDriverDevice(IDriverContext driverContext) : base(driverContext)
        {
            ShellyId = GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey);
            PollingInterval = GetPropertyValueInt("polling-interval");
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            _timer = new Timer(PollingInterval);


            var ret = await base.Start(token);

            if (!ret)
            {
                return ret;
            }

            if (!await Poll(token))
            {
                return false;
            }

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return true;
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Elapsed -= _timer_Elapsed;
            try
            {
                var ret = await Poll();
                if (!ret)
                {
                    PollFailCount++;
                }
                else
                {
                    PollFailCount = 0;
                }

                if (PollFailCount >= 10)
                {
                    DriverContext.Logger.LogError($"Stop polling, after 10 tries, we have some errors to check...");
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Error polling shelly device");
            }
            finally
            {
                _timer.Elapsed += _timer_Elapsed;
            }
        }

        public override Task<bool> Stop(CancellationToken token = new CancellationToken())
        {
            if (_timer != null)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }

            return base.Stop(token);
        }

        public abstract Task<bool> Write(int channelId, bool value, CancellationToken token = default);
        public abstract Task<bool> Poll(CancellationToken token = default);

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
