using System;
using System.Collections.Generic;
using System.Linq;

namespace P3.Logic.Operations.Energy.LoadManager
{
    internal sealed class LoadManagerLoad
    {
        public int Priority { get; set; }

        /// <summary>
        /// kW the load draws, 0 = load not used
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        /// Status of the load: true = running, false = idle, null = unknown (counts as running)
        /// </summary>
        public bool? Active { get; set; }

        public bool Shed { get; set; }

        public bool Configured => Power > 0;
    }

    internal sealed class LoadManagerSettings
    {
        public double MaxPower { get; set; } = LoadManagerLogicFactory.DefaultMaxPower;
        public double Hysteresis { get; set; } = LoadManagerLogicFactory.DefaultHysteresis;
        public TimeSpan ShedDelay { get; set; } = TimeSpan.FromSeconds(LoadManagerLogicFactory.DefaultShedDelay);
        public TimeSpan ReleaseDelay { get; set; } = TimeSpan.FromSeconds(LoadManagerLogicFactory.DefaultReleaseDelay);
    }

    /// <summary>
    /// Overload protection (Loxone "Load Manager"). Priority 1 is the most important load.
    /// <list type="bullet">
    /// <item>Total power above max power for the shed delay: running loads are shed (locked) lowest priority first, as many as the excess needs.
    /// Idle loads are skipped, shedding them frees nothing.</item>
    /// <item>After a shed the meter gets the shed delay again before more loads are shed.</item>
    /// <item>A shed load is released, highest priority first and one at a time, when the total power plus its power stays at or below
    /// max power - hysteresis for the release delay.</item>
    /// </list>
    /// </summary>
    internal sealed class LoadManagerEngine
    {
        public const int LoadCount = 8;

        private DateTimeOffset? _overSince;
        private DateTimeOffset? _releaseSince;
        private DateTimeOffset? _lastActionAt;

        public LoadManagerEngine()
        {
            for (var i = 0; i < LoadCount; i++)
            {
                Loads[i] = new LoadManagerLoad { Priority = i + 1 };
            }
        }

        public LoadManagerSettings Settings { get; } = new();
        public LoadManagerLoad[] Loads { get; } = new LoadManagerLoad[LoadCount];

        /// <summary>
        /// Total power in kW, null until measured
        /// </summary>
        public double? Power { get; set; }

        public bool Overload => Power.HasValue && Power.Value > Settings.MaxPower;

        /// <summary>
        /// kW left until max power (negative while overloaded)
        /// </summary>
        public double Reserve => Power.HasValue ? Settings.MaxPower - Power.Value : Settings.MaxPower;

        private IEnumerable<int> ByPriority() => Enumerable.Range(0, LoadCount).Where(i => Loads[i].Configured)
            .OrderBy(i => Loads[i].Priority).ThenBy(i => i);

        public void Evaluate(DateTimeOffset now)
        {
            // a load that is no longer configured must not stay locked
            foreach (var load in Loads.Where(l => !l.Configured))
            {
                load.Shed = false;
            }

            if (!Power.HasValue)
            {
                return;
            }

            if (Overload)
            {
                _releaseSince = null;
                _overSince ??= now;
                if (Elapsed(_overSince, Settings.ShedDelay, now) && (!_lastActionAt.HasValue || Elapsed(_lastActionAt, Settings.ShedDelay, now)))
                {
                    var excess = Power.Value - Settings.MaxPower;
                    foreach (var i in ByPriority().Reverse().Where(i => !Loads[i].Shed && Loads[i].Active != false))
                    {
                        if (excess <= 0)
                        {
                            break;
                        }
                        Loads[i].Shed = true;
                        excess -= Loads[i].Power;
                    }
                    _lastActionAt = now;
                    _overSince = null;
                }
                return;
            }

            _overSince = null;
            var next = ByPriority().Where(i => Loads[i].Shed).Cast<int?>().FirstOrDefault();
            if (!next.HasValue || Power.Value + Loads[next.Value].Power > Settings.MaxPower - Settings.Hysteresis)
            {
                _releaseSince = null;
                return;
            }

            _releaseSince ??= now;
            if (Elapsed(_releaseSince, Settings.ReleaseDelay, now) && (!_lastActionAt.HasValue || Elapsed(_lastActionAt, Settings.ReleaseDelay, now)))
            {
                Loads[next.Value].Shed = false;
                _lastActionAt = now;
                _releaseSince = null;
            }
        }

        private static bool Elapsed(DateTimeOffset? since, TimeSpan delay, DateTimeOffset now) => since.HasValue && now - since.Value >= delay;
    }
}
