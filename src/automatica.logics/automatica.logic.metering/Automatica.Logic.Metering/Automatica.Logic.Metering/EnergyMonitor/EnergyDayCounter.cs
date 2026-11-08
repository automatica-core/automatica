using System;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    public enum EnergyFlowKind
    {
        Pv = 0,
        GridImport = 1,
        GridExport = 2,
        BatteryCharge = 3,
        BatteryDischarge = 4
    }

    /// <summary>
    /// Today's energy (kWh) of each flow, starting again at local midnight.
    /// A flow with a meter (kWh counter) uses meter reading - reading at midnight. A flow without one integrates
    /// its power over time: the last power is held until the next value or tick (power values often come only on change).
    /// </summary>
    public sealed class EnergyDayCounter
    {
        private const int FlowCount = 5;

        private readonly TimeZoneInfo _timeZone;
        private readonly double[] _integrated = new double[FlowCount];
        private readonly double[] _power = new double[FlowCount];
        private readonly double?[] _meterBaseline = new double?[FlowCount];
        private readonly double?[] _meterLast = new double?[FlowCount];

        private DateTimeOffset? _lastUpdate;

        public EnergyDayCounter(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone ?? TimeZoneInfo.Local;
        }

        /// <summary>the local date the totals belong to, null before the first update</summary>
        public DateTime? Day { get; private set; }

        /// <summary>
        /// Books the held power up to <paramref name="now"/>. When the local day changed since the last update,
        /// the totals start again at midnight and only the part after midnight is booked.
        /// </summary>
        public void Advance(DateTimeOffset now)
        {
            var today = LocalDate(now);
            if (_lastUpdate == null || Day == null)
            {
                _lastUpdate = now;
                Day = today;
                return;
            }

            if (today > Day.Value)
            {
                StartDay(today);
                var midnight = LocalMidnight(today);
                if (midnight > _lastUpdate.Value)
                {
                    _lastUpdate = midnight;
                }
            }

            if (now <= _lastUpdate.Value)
            {
                // the clock went back (time sync): nothing to book, continue from the new time
                _lastUpdate = now;
                return;
            }

            var hours = (now - _lastUpdate.Value).TotalHours;
            for (var i = 0; i < FlowCount; i++)
            {
                _integrated[i] += _power[i] * hours;
            }
            _lastUpdate = now;
        }

        /// <summary>the power (kW) held from now on, call <see cref="Advance"/> first</summary>
        public void SetPower(EnergyFlowSnapshot snapshot)
        {
            _power[(int)EnergyFlowKind.Pv] = snapshot.Pv;
            _power[(int)EnergyFlowKind.GridImport] = snapshot.GridImport;
            _power[(int)EnergyFlowKind.GridExport] = snapshot.GridExport;
            _power[(int)EnergyFlowKind.BatteryCharge] = snapshot.BatteryCharge;
            _power[(int)EnergyFlowKind.BatteryDischarge] = snapshot.BatteryDischarge;
        }

        /// <summary>
        /// A meter reading (kWh, counting up) of a flow. The first reading of the day is the baseline. A reading below the
        /// last one (meter replaced or reset) keeps what was counted today and counts on from the new reading.
        /// </summary>
        public void SetMeter(EnergyFlowKind flow, double reading, DateTimeOffset now)
        {
            if (!double.IsFinite(reading))
            {
                return;
            }
            Advance(now);

            var i = (int)flow;
            if (_meterBaseline[i] == null || _meterLast[i] == null)
            {
                _meterBaseline[i] = reading;
            }
            else if (reading < _meterLast[i].Value)
            {
                var countedToday = _meterLast[i].Value - _meterBaseline[i].Value;
                _meterBaseline[i] = reading - countedToday;
            }
            _meterLast[i] = reading;
        }

        public bool HasMeter(EnergyFlowKind flow) => _meterLast[(int)flow].HasValue;

        /// <summary>today's energy of the flow in kWh</summary>
        public double Today(EnergyFlowKind flow)
        {
            var i = (int)flow;
            if (_meterLast[i].HasValue && _meterBaseline[i].HasValue)
            {
                return Math.Max(_meterLast[i].Value - _meterBaseline[i].Value, 0);
            }
            return _integrated[i];
        }

        /// <summary>energy used on site today (house + extra consumers)</summary>
        public double ConsumptionToday =>
            Math.Max(Today(EnergyFlowKind.Pv) + Today(EnergyFlowKind.GridImport) - Today(EnergyFlowKind.GridExport)
                     + Today(EnergyFlowKind.BatteryDischarge) - Today(EnergyFlowKind.BatteryCharge), 0);

        public double SelfConsumptionToday =>
            EnergyFlowCalculator.SelfConsumptionRate(Today(EnergyFlowKind.Pv), Today(EnergyFlowKind.GridExport));

        public double AutarkyToday =>
            EnergyFlowCalculator.AutarkyRate(ConsumptionToday, Today(EnergyFlowKind.GridImport));

        private void StartDay(DateTime day)
        {
            Day = day;
            for (var i = 0; i < FlowCount; i++)
            {
                _integrated[i] = 0;
                // the last reading of yesterday is where today starts
                _meterBaseline[i] = _meterLast[i];
            }
        }

        private DateTime LocalDate(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, _timeZone).Date;

        private DateTimeOffset LocalMidnight(DateTime day)
        {
            var local = DateTime.SpecifyKind(day, DateTimeKind.Unspecified);
            // midnight can fall into a DST gap in a few zones, take the first valid time after it
            while (_timeZone.IsInvalidTime(local))
            {
                local = local.AddMinutes(30);
            }
            return new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
        }
    }
}
