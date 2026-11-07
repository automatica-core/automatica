using Automatica.Core.Shading.Definition;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// Direct sun yes/no from a brightness value with two thresholds and delays, so passing clouds do not move the blinds.
    /// </summary>
    internal sealed class SunHysteresis
    {
        private DateTimeOffset? _candidateSince;

        public bool State { get; private set; }

        public bool Update(double? lux, SunThresholds thresholds, DateTimeOffset now)
        {
            if (!lux.HasValue)
            {
                // no brightness at all: no direct sun
                State = false;
                _candidateSince = null;
                return State;
            }

            var crossing = State ? lux.Value < thresholds.OffLux : lux.Value >= thresholds.OnLux;
            if (!crossing)
            {
                _candidateSince = null;
                return State;
            }

            _candidateSince ??= now;
            var delay = TimeSpan.FromMinutes(State ? thresholds.OffDelayMinutes : thresholds.OnDelayMinutes);
            if (now - _candidateSince.Value >= delay)
            {
                State = !State;
                _candidateSince = null;
            }

            return State;
        }

        /// <summary>
        /// Without hysteresis and delays (simulation)
        /// </summary>
        public static bool Instant(double? lux, SunThresholds thresholds) => lux.HasValue && lux.Value >= thresholds.OnLux;
    }
}
