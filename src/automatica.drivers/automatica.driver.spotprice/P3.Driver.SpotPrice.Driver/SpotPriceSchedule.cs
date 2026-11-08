using System;
using System.Collections.Generic;
using System.Linq;

namespace P3.Driver.SpotPrice.Driver
{
    /// <summary>
    /// A price slot, market price in EUR/MWh
    /// </summary>
    internal sealed record PriceSlot(DateTimeOffset Start, DateTimeOffset End, double Market)
    {
        public TimeSpan Duration => End - Start;
    }

    internal sealed record PriceInterval(DateTimeOffset Start, DateTimeOffset End);

    /// <summary>
    /// Known prices, sorted and without overlaps. Slot lengths come from the data (60 or 15 minutes).
    /// </summary>
    internal sealed class SpotPriceSchedule
    {
        public SpotPriceSchedule(IEnumerable<PriceSlot> slots)
        {
            var sorted = new List<PriceSlot>();
            foreach (var slot in slots.Where(s => s.End > s.Start && !double.IsNaN(s.Market)).OrderBy(s => s.Start).ThenBy(s => s.Duration))
            {
                // the first (finest) slot wins where two overlap
                if (sorted.Count > 0 && slot.Start < sorted[^1].End)
                {
                    continue;
                }
                sorted.Add(slot);
            }
            Slots = sorted;
        }

        public static SpotPriceSchedule Empty { get; } = new(Array.Empty<PriceSlot>());

        public IReadOnlyList<PriceSlot> Slots { get; }

        public DateTimeOffset? End => Slots.Count == 0 ? null : Slots[^1].End;

        public PriceSlot At(DateTimeOffset time) => Slots.FirstOrDefault(s => s.Start <= time && time < s.End);

        /// <summary>
        /// Time-weighted average market price of the known part of [from, to), null when nothing of it is known
        /// </summary>
        public double? Average(DateTimeOffset from, DateTimeOffset to)
        {
            double sum = 0, seconds = 0;
            foreach (var slot in Slots)
            {
                var start = slot.Start > from ? slot.Start : from;
                var end = slot.End < to ? slot.End : to;
                if (end <= start)
                {
                    continue;
                }
                var s = (end - start).TotalSeconds;
                sum += slot.Market * s;
                seconds += s;
            }
            return seconds > 0 ? sum / seconds : null;
        }

        public IEnumerable<PriceSlot> Between(DateTimeOffset from, DateTimeOffset to) => Slots.Where(s => s.Start >= from && s.Start < to);

        /// <summary>
        /// Picks the cheapest (or most expensive) slots of [from, to) that add up to <paramref name="duration"/>.
        /// A slot counts from <paramref name="from"/> on if it already started. Null when the known slots in the range are too short.
        /// </summary>
        public IReadOnlyList<PriceInterval> Plan(DateTimeOffset from, DateTimeOffset to, TimeSpan duration, bool contiguous, bool mostExpensive)
        {
            var candidates = Slots.Where(s => s.End > from && s.Start < to && s.End <= to).ToList();
            if (duration <= TimeSpan.Zero || candidates.Count == 0 || Sum(candidates) < duration)
            {
                return null;
            }

            var chosen = contiguous ? PlanContiguous(candidates, duration, mostExpensive) : PlanSlots(candidates, duration, mostExpensive);
            return chosen == null ? null : Merge(chosen);
        }

        private static TimeSpan Sum(IEnumerable<PriceSlot> slots) => slots.Aggregate(TimeSpan.Zero, (t, s) => t + s.Duration);

        private static List<PriceSlot> PlanSlots(List<PriceSlot> candidates, TimeSpan duration, bool mostExpensive)
        {
            var ordered = mostExpensive
                ? candidates.OrderByDescending(s => s.Market).ThenBy(s => s.Start)
                : candidates.OrderBy(s => s.Market).ThenBy(s => s.Start);
            var chosen = new List<PriceSlot>();
            var total = TimeSpan.Zero;
            foreach (var slot in ordered)
            {
                if (total >= duration)
                {
                    break;
                }
                chosen.Add(slot);
                total += slot.Duration;
            }
            return chosen.OrderBy(s => s.Start).ToList();
        }

        private static List<PriceSlot> PlanContiguous(List<PriceSlot> candidates, TimeSpan duration, bool mostExpensive)
        {
            List<PriceSlot> best = null;
            double bestAverage = 0;
            for (var first = 0; first < candidates.Count; first++)
            {
                var window = new List<PriceSlot> { candidates[first] };
                var total = candidates[first].Duration;
                for (var next = first + 1; total < duration && next < candidates.Count && candidates[next].Start == candidates[next - 1].End; next++)
                {
                    window.Add(candidates[next]);
                    total += candidates[next].Duration;
                }
                if (total < duration)
                {
                    continue;
                }
                var average = window.Sum(s => s.Market * s.Duration.TotalSeconds) / total.TotalSeconds;
                // strictly better only: ties keep the earlier window
                if (best == null || (mostExpensive ? average > bestAverage + 1e-9 : average < bestAverage - 1e-9))
                {
                    best = window;
                    bestAverage = average;
                }
            }
            return best;
        }

        private static IReadOnlyList<PriceInterval> Merge(List<PriceSlot> slots)
        {
            var result = new List<PriceInterval>();
            foreach (var slot in slots)
            {
                if (result.Count > 0 && result[^1].End == slot.Start)
                {
                    result[^1] = result[^1] with { End = slot.End };
                }
                else
                {
                    result.Add(new PriceInterval(slot.Start, slot.End));
                }
            }
            return result;
        }
    }

    /// <summary>
    /// A cheapest/most expensive window: N hours per period of L hours. The first period starts when prices are known,
    /// the next one when the period ends. While the plan has not started it is computed again when new prices arrive;
    /// once it started it is kept, so the load runs exactly N hours per period.
    /// </summary>
    internal sealed class PriceWindowPlanner
    {
        private DateTimeOffset? _periodEnd;
        private long _plannedVersion = -1;

        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(3);
        public TimeSpan Period { get; set; } = TimeSpan.FromHours(24);
        public bool Contiguous { get; set; } = true;
        public bool MostExpensive { get; set; }

        public IReadOnlyList<PriceInterval> Plan { get; private set; }

        public bool Started(DateTimeOffset now) => Plan != null && Plan.Count > 0 && Plan[0].Start <= now;

        /// <summary>
        /// Updates the plan, returns true while now is inside it
        /// </summary>
        public bool Evaluate(SpotPriceSchedule schedule, long version, DateTimeOffset now)
        {
            if (!_periodEnd.HasValue || now >= _periodEnd.Value)
            {
                _periodEnd = now + Period;
                Plan = null;
                _plannedVersion = -1;
            }

            if (version != _plannedVersion && !Started(now))
            {
                Plan = schedule.Plan(now, _periodEnd.Value, Duration, Contiguous, MostExpensive);
                // without enough prices the period has not begun: it starts with the first plan
                if (Plan == null)
                {
                    _periodEnd = null;
                }
                _plannedVersion = version;
            }

            return Plan != null && Plan.Any(i => i.Start <= now && now < i.End);
        }

        public void Reset()
        {
            _periodEnd = null;
            _plannedVersion = -1;
            Plan = null;
        }
    }
}
