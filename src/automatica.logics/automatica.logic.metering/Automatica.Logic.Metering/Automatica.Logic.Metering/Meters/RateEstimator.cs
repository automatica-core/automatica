using System;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Power (or flow) derived from energy steps: pulses or the difference of two readings, divided by the time in between.
    /// Without a new step the value falls: it is never more than the last step spread over the time since it came, and
    /// 0 after <see cref="Silence"/> (also when only one step came in).
    /// </summary>
    public sealed class RateEstimator
    {
        public static readonly TimeSpan Silence = TimeSpan.FromHours(1);

        private DateTimeOffset? _lastStep;
        private double _lastQuantity;
        private double? _rate;

        /// <summary>a step of <paramref name="quantity"/> (null: only the starting point, e.g. the first reading)</summary>
        public void Add(double? quantity, DateTimeOffset now)
        {
            if (quantity is { } q && double.IsFinite(q) && q >= 0 && _lastStep is { } last && now > last)
            {
                _rate = q / (now - last).TotalHours;
                _lastQuantity = q;
            }
            _lastStep = now;
        }

        /// <summary>the current rate per hour, null until two steps came in</summary>
        public double? Current(DateTimeOffset now)
        {
            if (_lastStep is not { } last)
            {
                return null;
            }
            var since = now - last;
            if (since >= Silence)
            {
                return 0;
            }
            if (_rate is not { } rate)
            {
                return null;
            }
            if (since <= TimeSpan.Zero)
            {
                return rate;
            }
            return Math.Min(rate, _lastQuantity / since.TotalHours);
        }
    }
}
