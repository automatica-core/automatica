using System;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.Times.DriverFactory.DateTime
{
    public class DateTimeNode : DriverBase
    {
        private readonly Func<object> _getValue;
        private readonly Timer _timer = new Timer(1000);

        public DateTimeNode(IDriverContext driverContext, Func<object> getValue) : base(driverContext)
        {
            _getValue = getValue;
        }

        public override Task<bool> Start()
        {
            if (!DriverContext.IsTest)
            {
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();
            }

            DispatchValue(_getValue());

            return base.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DispatchValue(_getValue());
        }

        public override Task<bool> Stop()
        {
            if (!DriverContext.IsTest)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
            }
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
