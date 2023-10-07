using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Timer = System.Timers.Timer;

namespace P3.Driver.Times.DriverFactory.DateTime
{
    public class DateTimeNode<T> : DriverNotWriteableBase
    {
        private readonly Func<T> _getValue;
        private readonly Timer _timer = new Timer(1000);
        private T _prevValue = default(T);

        public DateTimeNode(IDriverContext driverContext, Func<T> getValue) : base(driverContext)
        {
            _getValue = getValue;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            DispatchRead(_getValue());
            return Task.FromResult(true);
        }

        protected override Task<bool> StartedInternal(CancellationToken token = new CancellationToken())
        {
            if (!DriverContext.IsTest)
            {
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();
            }

            DispatchRead(_getValue());
            return Task.FromResult(true);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var value = _getValue();

            if (!EqualityComparer<T>.Default.Equals(value, _prevValue))
            {
                DispatchRead(_getValue());
                _prevValue = value;
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            if (!DriverContext.IsTest)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
            }
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
