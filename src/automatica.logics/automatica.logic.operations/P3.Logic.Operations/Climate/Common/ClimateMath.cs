using System;
using System.Collections.Generic;
using System.Linq;

namespace P3.Logic.Operations.Climate.Common
{
    /// <summary>
    /// PI controller with output limits. While the output sits on a limit, the integral only grows up to the value
    /// that puts the output exactly on the limit (back-calculation anti-windup), so a pure I controller still reaches it.
    /// </summary>
    public sealed class PiController
    {
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Min { get; set; }
        public double Max { get; set; } = 100;
        public double Integral { get; private set; }

        public double Step(double error, double dtSeconds)
        {
            var low = Math.Min(Min, Max);
            var high = Math.Max(Min, Max);
            var integral = Integral + Ki * error * dtSeconds;
            var output = Kp * error + integral;

            var limit = output > high && Ki * error > 0 ? high
                : output < low && Ki * error < 0 ? low
                : (double?)null;
            if (limit.HasValue)
            {
                var atLimit = limit.Value - Kp * error;
                integral = Math.Clamp(atLimit, Math.Min(Integral, integral), Math.Max(Integral, integral));
            }

            Integral = integral;
            return Math.Clamp(Kp * error + Integral, low, high);
        }

        public void Reset(double integral = 0)
        {
            Integral = integral;
        }
    }

    public static class HeatingCurveMath
    {
        /// <summary>
        /// Heating curve as published on de.wikipedia.org/wiki/Heizkurve (the source the Loxone heating curve refers to):
        /// Ft = Tt + level - slope * DAR * (1.4347 + 0.021 * DAR + 247.9e-6 * DAR²), DAR = outdoor - Tt.
        /// At or above the room setpoint no heating is needed: Ft = Tt + level.
        /// </summary>
        public static double Heating(double roomSetpoint, double outdoor, double slope, double level)
        {
            var dar = outdoor - roomSetpoint;
            if (dar >= 0)
            {
                return roomSetpoint + level;
            }
            return roomSetpoint + level - slope * dar * (1.4347 + 0.021 * dar + 247.9e-6 * dar * dar);
        }

        /// <summary>
        /// Linear cooling curve: the warmer it is outside, the colder the flow. Ft = Tt - level - slope * (outdoor - Tt).
        /// Below the room setpoint the flow stays at Tt - level.
        /// </summary>
        public static double Cooling(double roomSetpoint, double outdoor, double slope, double level)
        {
            var dar = Math.Max(0, outdoor - roomSetpoint);
            return roomSetpoint - level - slope * dar;
        }

        /// <summary>
        /// Magnus formula with the coefficients over water (above 0 °C) and over ice (below), as used by the Loxone dewpoint calculator
        /// </summary>
        public static double? DewPoint(double temperature, double relativeHumidity)
        {
            if (temperature < -65 || temperature > 60 || relativeHumidity <= 0)
            {
                return null;
            }
            var h = Math.Min(relativeHumidity, 100);
            var (k2, k3) = temperature >= 0 ? (17.62, 243.12) : (22.46, 272.62);
            var ln = Math.Log(h / 100);
            return k3 * ((k2 * temperature) / (k3 + temperature) + ln) / ((k2 * k3) / (k3 + temperature) - ln);
        }
    }

    /// <summary>
    /// Average outdoor temperature of the last 48 hours, one sample per 10 minutes. Known once 24 hours are recorded.
    /// The history is kept in memory only and starts again after a restart of the logic.
    /// </summary>
    public sealed class OutdoorAverage
    {
        private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan Window = TimeSpan.FromHours(48);
        private static readonly TimeSpan MinimumSpan = TimeSpan.FromHours(24);

        private readonly Queue<(DateTime At, double Value)> _samples = new();
        private DateTime? _first;

        public void Add(DateTime now, double value)
        {
            if (_samples.Count > 0 && now - _samples.Last().At < SampleInterval)
            {
                return;
            }
            _first ??= now;
            _samples.Enqueue((now, value));
            while (_samples.Count > 0 && now - _samples.Peek().At > Window)
            {
                _samples.Dequeue();
            }
        }

        public double? Value(DateTime now)
        {
            if (!_first.HasValue || now - _first.Value < MinimumSpan || _samples.Count == 0)
            {
                return null;
            }
            return _samples.Average(a => a.Value);
        }
    }

    public enum SourceMode
    {
        Off = 0,
        Heating = 1,
        Cooling = -1
    }

    /// <summary>
    /// Heating/cooling decision of a central source (heating and cooling controller, HVAC, AC central, fan coil central):
    /// starts when the load of a mode reaches its switch-on threshold (the larger load wins, heating on a tie),
    /// keeps running while there is any load of that mode, keeps the minimum runtime and waits the restart delay after a stop.
    /// Changing from heating to cooling or back always goes through off.
    /// </summary>
    public sealed class Changeover
    {
        public SourceMode Mode { get; private set; } = SourceMode.Off;
        public DateTime? Since { get; private set; }
        public DateTime? StoppedAt { get; private set; }

        public double HeatThreshold { get; set; } = 30;
        public double CoolThreshold { get; set; } = 30;
        public double MinHeatRunSeconds { get; set; }
        public double MinCoolRunSeconds { get; set; }
        public double RestartDelaySeconds { get; set; } = 180;

        public double RunSeconds(DateTime now) => Mode == SourceMode.Off || !Since.HasValue ? 0 : (now - Since.Value).TotalSeconds;

        /// <param name="force">stop at once, without the minimum runtime (block switched off or locked)</param>
        public SourceMode Update(DateTime now, double heatLoad, double coolLoad, bool heatAllowed, bool coolAllowed, bool force = false)
        {
            if (force)
            {
                if (Mode != SourceMode.Off)
                {
                    StopAt(now);
                }
                return Mode;
            }

            var wantHeat = heatAllowed && heatLoad > 0 && heatLoad >= HeatThreshold;
            var wantCool = coolAllowed && coolLoad > 0 && coolLoad >= CoolThreshold;

            switch (Mode)
            {
                case SourceMode.Off:
                    if (StoppedAt.HasValue && (now - StoppedAt.Value).TotalSeconds < RestartDelaySeconds)
                    {
                        break;
                    }
                    if (wantHeat && (!wantCool || heatLoad >= coolLoad))
                    {
                        StartAt(now, SourceMode.Heating);
                    }
                    else if (wantCool)
                    {
                        StartAt(now, SourceMode.Cooling);
                    }
                    break;
                case SourceMode.Heating:
                {
                    var keep = heatAllowed && heatLoad > 0 && !(wantCool && coolLoad > heatLoad);
                    if (!keep && RunSeconds(now) >= MinHeatRunSeconds)
                    {
                        StopAt(now);
                    }
                    break;
                }
                case SourceMode.Cooling:
                {
                    var keep = coolAllowed && coolLoad > 0 && !(wantHeat && heatLoad > coolLoad);
                    if (!keep && RunSeconds(now) >= MinCoolRunSeconds)
                    {
                        StopAt(now);
                    }
                    break;
                }
            }
            return Mode;
        }

        private void StartAt(DateTime now, SourceMode mode)
        {
            Mode = mode;
            Since = now;
        }

        private void StopAt(DateTime now)
        {
            Mode = SourceMode.Off;
            Since = null;
            StoppedAt = now;
        }
    }

    /// <summary>
    /// Mode and outdoor limits of a central source, shared by the heating and cooling controller, the AC central and the fan coil central
    /// </summary>
    public static class SourceLimits
    {
        /// <summary>
        /// Outdoor temperature mode: 0 = limits not used, 1 = average of the last 48 h (current value until 24 h are recorded), 2 = current value
        /// </summary>
        public static double? UsedOutdoor(int outdoorMode, double? current, double? average)
        {
            return outdoorMode switch
            {
                0 => null,
                1 => average ?? current,
                _ => current
            };
        }

        /// <param name="mode">0 = off, 1 = automatic, 2 = heating only, 3 = cooling only</param>
        public static (bool Heat, bool Cool) Allowed(int mode, double? outdoor, double limitHeating, double limitCooling)
        {
            var heat = mode == 1 || mode == 2;
            var cool = mode == 1 || mode == 3;
            if (outdoor.HasValue)
            {
                heat &= outdoor.Value <= limitHeating;
                cool &= outdoor.Value >= limitCooling;
            }
            return (heat, cool);
        }

        /// <summary>
        /// 0 = none, 1 = heating only, 2 = cooling only, 3 = heating or cooling
        /// </summary>
        public static int AvailableMode(bool heat, bool cool) => (heat ? 1 : 0) + (cool ? 2 : 0);
    }
}
