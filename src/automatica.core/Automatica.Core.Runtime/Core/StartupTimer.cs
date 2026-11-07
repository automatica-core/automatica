using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Measures the phases of a (re)start and the drivers that take long to configure or start.
    /// The summary is a warning when the start is slow, so it is also in the logs of installations that only log warnings.
    /// </summary>
    internal class StartupTimer
    {
        internal static readonly TimeSpan SlowStart = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan SlowItem = TimeSpan.FromSeconds(2);

        private readonly Stopwatch _total = Stopwatch.StartNew();
        private readonly List<(string Phase, TimeSpan Duration)> _phases = new();
        private readonly ConcurrentBag<(string Item, TimeSpan Duration)> _slowItems = new();

        public TimeSpan Total => _total.Elapsed;

        public async Task Measure(string phase, Func<Task> action)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                AddPhase(phase, watch.Elapsed);
            }
        }

        public async Task<T> Measure<T>(string phase, Func<Task<T>> action)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                return await action();
            }
            finally
            {
                AddPhase(phase, watch.Elapsed);
            }
        }

        /// <summary>
        /// A single item (e.g. one driver), only kept when it is slow
        /// </summary>
        public async Task MeasureItem(string item, Func<Task> action)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                AddItem(item, watch.Elapsed);
            }
        }

        internal void AddPhase(string phase, TimeSpan duration)
        {
            lock (_phases)
            {
                _phases.Add((phase, duration));
            }
        }

        internal void AddItem(string item, TimeSpan duration)
        {
            if (duration >= SlowItem)
            {
                _slowItems.Add((item, duration));
            }
        }

        internal string Summary(string name, TimeSpan total)
        {
            List<(string Phase, TimeSpan Duration)> phases;
            lock (_phases)
            {
                phases = _phases.ToList();
            }

            var text = $"{name} took {Format(total)}: " + string.Join(", ", phases.Select(a => $"{a.Phase} {Format(a.Duration)}"));
            var slow = _slowItems.OrderByDescending(a => a.Duration).Take(10).ToList();
            if (slow.Count > 0)
            {
                text += "; slowest: " + string.Join(", ", slow.Select(a => $"{a.Item} {Format(a.Duration)}"));
            }
            return text;
        }

        public void Log(ILogger logger, string name)
        {
            var total = Total;
            var summary = Summary(name, total);
            if (total >= SlowStart)
            {
                logger.LogWarning(summary);
            }
            else
            {
                logger.LogInformation(summary);
            }
        }

        private static string Format(TimeSpan duration)
        {
            var format = duration.TotalSeconds >= 10 ? "0" : "0.0";
            return duration.TotalSeconds.ToString(format, CultureInfo.InvariantCulture) + "s";
        }
    }
}
