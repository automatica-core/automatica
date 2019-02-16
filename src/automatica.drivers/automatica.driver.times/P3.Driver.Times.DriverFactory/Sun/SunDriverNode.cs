using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Innovative.Geometry;
using Innovative.SolarCalculator;

namespace P3.Driver.Times.DriverFactory.Sun
{
    public class SunDriverNode : DriverBase
    {
        private readonly Func<SolarTimes, System.DateTime, object> _getValueFunc;
        private Timer _tickTimer;
        internal static readonly Angle Latitude = new Angle(BaseTimesDriverFactory.Latitude);
        internal static readonly Angle Longitude = new Angle(BaseTimesDriverFactory.Longitude);
        private readonly object _lock = new object();

        public SunDriverNode(IDriverContext driverContext, Func<SolarTimes, System.DateTime, object> getValueFunc) : base(driverContext)
        {
            _getValueFunc = getValueFunc;
        }

        private void TimerTick(object state)
        {
            lock (_lock)
            {
                DispatchSolarValue();
            }
        }

        private void DispatchSolarValue()
        {
            var solarTimes = new SolarTimes(System.DateTime.Now, Latitude, Longitude);
            DispatchValue(_getValueFunc(solarTimes, System.DateTime.Now));
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            if(value is System.DateTime)
            {
                var solarTimes = new SolarTimes((System.DateTime)value, Latitude, Longitude);
                DispatchValue(_getValueFunc(solarTimes, (System.DateTime)value));
            }
            return base.WriteValue(source, value);
        }

        public override Task<bool> Start()
        {
            DispatchSolarValue();

            if (!DriverContext.IsTest)
            {
                _tickTimer = new Timer(TimerTick, this, 1000, 1000);
             
            }
            return base.Start();
        }

        public override Task<bool> Stop()
        {
            if (!DriverContext.IsTest)
            {
                _tickTimer.Dispose();
            }
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
