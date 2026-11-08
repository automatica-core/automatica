using System;
using System.Collections.Generic;
using System.Linq;
using P3.Driver.SpotPrice.Driver;
using Xunit;

namespace P3.Driver.SpotPrice.Tests
{
    public class SpotPriceScheduleTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 11, 8, 0, 0, 0, TimeSpan.Zero);

        private static SpotPriceSchedule Hourly(params double[] prices) =>
            new(prices.Select((p, i) => new PriceSlot(T0.AddHours(i), T0.AddHours(i + 1), p)));

        private static SpotPriceSchedule Quarterly(params double[] prices) =>
            new(prices.Select((p, i) => new PriceSlot(T0.AddMinutes(15 * i), T0.AddMinutes(15 * (i + 1)), p)));

        private static (double, double)[] Hours(IReadOnlyList<PriceInterval> plan) =>
            plan.Select(i => ((i.Start - T0).TotalHours, (i.End - T0).TotalHours)).ToArray();

        [Fact]
        public void Contiguous_PicksTheCheapestBlock()
        {
            var schedule = Hourly(90, 80, 60, 50, 55, 70, 100);
            var plan = schedule.Plan(T0, T0.AddHours(7), TimeSpan.FromHours(3), contiguous: true, mostExpensive: false);
            Assert.Equal(new[] { (2d, 5d) }, Hours(plan));
        }

        [Fact]
        public void Contiguous_TieKeepsTheEarlierBlock()
        {
            var schedule = Hourly(50, 50, 90, 50, 50);
            var plan = schedule.Plan(T0, T0.AddHours(5), TimeSpan.FromHours(2), true, false);
            Assert.Equal(new[] { (0d, 2d) }, Hours(plan));
        }

        [Fact]
        public void Contiguous_DoesNotBridgeGaps()
        {
            var slots = new[]
            {
                new PriceSlot(T0, T0.AddHours(1), 10),
                new PriceSlot(T0.AddHours(2), T0.AddHours(3), 10),
                new PriceSlot(T0.AddHours(3), T0.AddHours(4), 40),
            };
            var plan = new SpotPriceSchedule(slots).Plan(T0, T0.AddHours(4), TimeSpan.FromHours(2), true, false);
            Assert.Equal(new[] { (2d, 4d) }, Hours(plan));
        }

        [Fact]
        public void Single_PicksTheCheapestSlots_AndMergesNeighbours()
        {
            var schedule = Hourly(90, 20, 30, 80, 10, 70);
            var plan = schedule.Plan(T0, T0.AddHours(6), TimeSpan.FromHours(3), false, false);
            Assert.Equal(new[] { (1d, 3d), (4d, 5d) }, Hours(plan));
        }

        [Fact]
        public void MostExpensive_PicksThePeak()
        {
            var schedule = Hourly(90, 20, 30, 180, 170, 70);
            var plan = schedule.Plan(T0, T0.AddHours(6), TimeSpan.FromHours(2), true, true);
            Assert.Equal(new[] { (3d, 5d) }, Hours(plan));
        }

        [Fact]
        public void QuarterHours_AreUsed_ForFractionalHours()
        {
            var schedule = Quarterly(50, 50, 10, 10, 10, 10, 10, 10, 50, 50);
            var plan = schedule.Plan(T0, T0.AddHours(3), TimeSpan.FromHours(1.5), true, false);
            Assert.Equal(new[] { (0.5, 2d) }, Hours(plan));
        }

        [Fact]
        public void TooFewKnownPrices_NoPlan()
        {
            var schedule = Hourly(10, 20);
            Assert.Null(schedule.Plan(T0, T0.AddHours(24), TimeSpan.FromHours(3), true, false));
        }

        [Fact]
        public void SlotsCrossingThePeriodEnd_AreNotUsed()
        {
            var schedule = Hourly(90, 90, 10);
            Assert.Null(schedule.Plan(T0, T0.AddHours(2.5), TimeSpan.FromHours(3), false, false));
        }

        [Fact]
        public void Overlap_TheFinerSlotWins()
        {
            var slots = new List<PriceSlot> { new(T0, T0.AddHours(1), 100) };
            slots.AddRange(Enumerable.Range(0, 4).Select(i => new PriceSlot(T0.AddMinutes(15 * i), T0.AddMinutes(15 * (i + 1)), 10 + i)));
            var schedule = new SpotPriceSchedule(slots);
            Assert.Equal(4, schedule.Slots.Count);
            Assert.Equal(11.5, schedule.Average(T0, T0.AddHours(1)));
        }

        [Fact]
        public void Average_IsTimeWeighted_OverTheKnownPart()
        {
            var schedule = Hourly(10, 40);
            Assert.Equal(25, schedule.Average(T0, T0.AddHours(2)));
            Assert.Equal(25, schedule.Average(T0.AddMinutes(30), T0.AddHours(1.5)));
            Assert.Equal(40, schedule.Average(T0.AddHours(1), T0.AddHours(5)));
            Assert.Null(schedule.Average(T0.AddHours(3), T0.AddHours(4)));
        }

        [Fact]
        public void Planner_IsOnExactlyTheGivenHoursPerPeriod()
        {
            // 72 hours with a cheap night every day
            var prices = Enumerable.Range(0, 72).Select(h => (h % 24) is >= 2 and < 6 ? 20d : 100d + h % 24).ToArray();
            var schedule = Hourly(prices);
            var planner = new PriceWindowPlanner { Duration = TimeSpan.FromHours(3), Period = TimeSpan.FromHours(24) };

            var on = 0;
            for (var t = T0; t < T0.AddHours(48); t = t.AddMinutes(15))
            {
                if (planner.Evaluate(schedule, 1, t))
                {
                    on++;
                }
            }
            Assert.Equal(2 * 3 * 4, on);
        }

        [Fact]
        public void Planner_NewPricesChangeAPlanThatHasNotStarted()
        {
            var planner = new PriceWindowPlanner { Duration = TimeSpan.FromHours(1), Period = TimeSpan.FromHours(24) };
            Assert.False(planner.Evaluate(Hourly(50, 40, 30, 20), 1, T0));
            Assert.Equal(T0.AddHours(3), planner.Plan[0].Start);

            // tomorrow arrives with a cheaper hour
            Assert.False(planner.Evaluate(Hourly(50, 40, 30, 20, 25, 5), 2, T0.AddHours(1)));
            Assert.Equal(T0.AddHours(5), planner.Plan[0].Start);
        }

        [Fact]
        public void Planner_AStartedPlanIsKept()
        {
            var planner = new PriceWindowPlanner { Duration = TimeSpan.FromHours(2), Period = TimeSpan.FromHours(24) };
            Assert.True(planner.Evaluate(Hourly(10, 10, 50, 50), 1, T0));
            Assert.True(planner.Evaluate(Hourly(10, 10, 50, 50, 1, 1), 2, T0.AddHours(1)));
            Assert.Equal(T0, planner.Plan[0].Start);
            Assert.False(planner.Evaluate(Hourly(10, 10, 50, 50, 1, 1), 2, T0.AddHours(4)));
        }

        [Fact]
        public void Planner_WithoutEnoughPrices_IsOff_AndPlansWhenTheyArrive()
        {
            var planner = new PriceWindowPlanner { Duration = TimeSpan.FromHours(3), Period = TimeSpan.FromHours(24) };
            Assert.False(planner.Evaluate(Hourly(10), 1, T0));
            Assert.Null(planner.Plan);
            Assert.True(planner.Evaluate(Hourly(10, 10, 10, 10), 2, T0.AddMinutes(30)));
        }
    }
}
