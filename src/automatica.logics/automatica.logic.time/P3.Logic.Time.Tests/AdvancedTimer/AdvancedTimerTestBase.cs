using System;
using System.Linq;
using Automatica.Core.EF.Models;
using P3.Logic.Time.AdvancedTimer;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.AdvancedTimer
{
    public abstract class AdvancedTimerTestBase : TimeLogicTest<AdvancedTimerRuleFactory>
    {
        protected static readonly Guid Output = AdvancedTimerRuleFactory.RuleOutput;

        protected AdvancedTimerTestBase(TimeZoneInfo zone) : base(zone)
        {
        }

        protected static DateTime Utc(int year, int month, int day, int hour, int minute = 0, int second = 0, int millisecond = 0)
        {
            return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
        }

        /// <summary>
        /// An entry as the UI sends it: start and end as UTC instants on the day the entry was created
        /// </summary>
        protected static CalendarPropertyDataEntry Entry(DateTime startUtc, DateTime endUtc, string recurrence = "FREQ=DAILY")
        {
            return new CalendarPropertyDataEntry
            {
                Text = "entry",
                StartDate = startUtc,
                EndDate = endUtc,
                RecurrenceRule = recurrence,
                AllDay = false
            };
        }

        protected void StartWith(DateTime utcNow, params CalendarPropertyDataEntry[] entries)
        {
            SetUtcNow(utcNow);
            Interface(AdvancedTimerRuleFactory.RuleTimerParameter).Value = new CalendarPropertyData { Value = entries.ToList() };
            Start();
        }

        protected bool? State => (bool?)Last(Output);

        protected double IntervalMs => SutTimer.Interval;

        protected void AssertState(bool expected)
        {
            Assert.NotEmpty(Values(Output));
            Assert.Equal(expected, State);
        }

        /// <summary>
        /// The next tick is scheduled in <paramref name="expected"/> (a millisecond of rounding is allowed)
        /// </summary>
        protected void AssertNextTickIn(TimeSpan expected)
        {
            Assert.InRange(IntervalMs, expected.TotalMilliseconds - 1, expected.TotalMilliseconds + 1);
        }

        /// <summary>
        /// Moves the fixed clock and lets the scheduled tick run
        /// </summary>
        protected void TickAt(DateTime utcNow)
        {
            SetUtcNow(utcNow);
            FireTick();
        }
    }
}
