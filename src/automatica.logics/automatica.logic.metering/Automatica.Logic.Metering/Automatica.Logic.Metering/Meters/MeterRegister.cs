using System;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// One counting direction of a meter (consumption, import, export, charge, ...): energy today, this month and in total.
    /// Fed by power (held until the next value and integrated over time), by an absolute meter reading or by energy steps
    /// (pulses). Today starts again at local midnight, the month on the first of the month.
    /// Once a reading came in the register counts the readings only, power is then just shown.
    /// </summary>
    public sealed class MeterRegister
    {
        private readonly TimeZoneInfo _timeZone;
        private double _power;
        private DateTimeOffset? _lastUpdate;

        public MeterRegister(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone ?? TimeZoneInfo.Local;
        }

        public double Today { get; private set; }
        public double Month { get; private set; }
        public double Total { get; private set; }

        /// <summary>the local date <see cref="Today"/> belongs to, null before the first update</summary>
        public DateTime? Day { get; private set; }

        /// <summary>the last absolute reading, the next reading counts from there (also after a restart)</summary>
        public double? LastReading { get; private set; }

        /// <summary>true once a reading came in: power is no longer integrated</summary>
        public bool ReadingMode { get; private set; }

        /// <summary>
        /// Books the held power up to <paramref name="now"/>. Each passed midnight starts a new day (and month), the power
        /// is split at midnight so each day gets its own part.
        /// </summary>
        public void Advance(DateTimeOffset now)
        {
            var today = LocalDate(now);
            if (Day == null)
            {
                Day = today;
                _lastUpdate = now;
                return;
            }
            _lastUpdate ??= now;

            while (Day.Value < today)
            {
                var next = Day.Value.AddDays(1);
                var midnight = LocalMidnight(next);
                if (midnight > _lastUpdate.Value)
                {
                    Book(_power * (midnight - _lastUpdate.Value).TotalHours);
                    _lastUpdate = midnight;
                }
                StartDay(next);
            }

            if (now <= _lastUpdate.Value)
            {
                // the clock went back (time sync): nothing to book, continue from the new time
                _lastUpdate = now;
                return;
            }

            Book(_power * (now - _lastUpdate.Value).TotalHours);
            _lastUpdate = now;
        }

        /// <summary>the power held from now on (energy per hour, negative counts as 0); ignored for counting in reading mode</summary>
        public void SetPower(double power, DateTimeOffset now)
        {
            Advance(now);
            _power = ReadingMode || !double.IsFinite(power) ? 0 : Math.Max(power, 0);
        }

        /// <summary>adds an energy step (e.g. one pulse)</summary>
        public void AddEnergy(double energy, DateTimeOffset now)
        {
            Advance(now);
            Book(energy);
        }

        /// <summary>
        /// An absolute meter reading, counting up. The first reading ever is the baseline (and the total starts there, so
        /// the total follows the meter). A reading below the last one (meter replaced or reset) is the new baseline.
        /// Returns the energy counted by this reading, null for a baseline.
        /// </summary>
        public double? SetReading(double reading, DateTimeOffset now)
        {
            if (!double.IsFinite(reading))
            {
                return null;
            }
            Advance(now);
            ReadingMode = true;
            _power = 0;

            if (LastReading == null)
            {
                LastReading = reading;
                if (Total == 0 && reading > 0)
                {
                    Total = reading;
                }
                return null;
            }

            var delta = reading - LastReading.Value;
            LastReading = reading;
            if (delta < 0)
            {
                return null;
            }
            Book(delta);
            return delta;
        }

        public MeterRegisterState Save() => new()
        {
            Today = Today,
            Month = Month,
            Total = Total,
            Day = Day?.ToString("yyyy-MM-dd"),
            LastReading = LastReading,
            ReadingMode = ReadingMode
        };

        /// <summary>restores a saved state; the time in between is not integrated (the power during the downtime is unknown)</summary>
        public void Load(MeterRegisterState state, DateTimeOffset now)
        {
            if (state == null)
            {
                return;
            }
            Today = Finite(state.Today);
            Month = Finite(state.Month);
            Total = Finite(state.Total);
            LastReading = state.LastReading is { } r && double.IsFinite(r) ? r : null;
            ReadingMode = state.ReadingMode;
            Day = DateTime.TryParseExact(state.Day, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var day) ? day : null;
            _power = 0;
            _lastUpdate = now;
            if (Day == null)
            {
                Today = 0;
                Month = 0;
            }
            Advance(now);
        }

        private static double Finite(double value) => double.IsFinite(value) && value > 0 ? value : 0;

        private void Book(double energy)
        {
            if (!(energy > 0) || !double.IsFinite(energy))
            {
                return;
            }
            Today += energy;
            Month += energy;
            Total += energy;
        }

        private void StartDay(DateTime day)
        {
            if (Day == null || day.Month != Day.Value.Month || day.Year != Day.Value.Year)
            {
                Month = 0;
            }
            Today = 0;
            Day = day;
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

    public sealed class MeterRegisterState
    {
        public double Today { get; set; }
        public double Month { get; set; }
        public double Total { get; set; }
        public string Day { get; set; }
        public double? LastReading { get; set; }
        public bool ReadingMode { get; set; }
    }
}
