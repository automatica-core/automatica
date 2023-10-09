using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Timer = System.Timers.Timer;

namespace Automatica.Driver.ShellyFactory
{
    internal abstract class ShellyDriverDevice : DriverBase
    {
        private Timer _timer;
        public string ShellyId { get; }
        public int PollingInterval { get; }

        protected ShellyDriverDevice(IDriverContext driverContext) : base(driverContext)
        {
            ShellyId = GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey);
            PollingInterval = GetPropertyValueInt("polling-interval");
        }

        public override Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            _timer = new Timer(PollingInterval);

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

            return base.Start(token);
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Elapsed -= _timer_Elapsed;
            try
            {
                await Poll();
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
