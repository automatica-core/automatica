using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.PulseAt;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Time.PulseAt.PulseAtLogicFactory;

namespace P3.Logic.Time.Tests.PulseAt
{
    /// <summary>
    /// Pulse at in Europe/Vienna. 2026-11-08 is a Sunday.
    /// </summary>
    public class PulseAtTests : TimeLogicTest<PulseAtLogicFactory>
    {
        public PulseAtTests() : base(TimeZones.Vienna)
        {
        }

        private static DateTime V(int year, int month, int day, int hour, int minute = 0, int second = 0) => TimeZones.ViennaToUtc(year, month, day, hour, minute, second);

        private PulseAtRule Rule => (PulseAtRule)Sut;

        private DateTime? NextLocal => (DateTime?)Last(F.OutputNext);

        private void Configure(PulseAtType type, string time = "07:00:00", double offset = 0, bool oneTime = false, string date = "", double duration = 1)
        {
            Interface(F.ParamType).Value = ((int)type).ToString();
            Interface(F.ParamTime).Value = time;
            Interface(F.ParamOffset).Value = offset;
            Interface(F.ParamOneTime).Value = oneTime;
            Interface(F.ParamDate).Value = date;
            Interface(F.ParamDuration).Value = duration;
            Start();
        }

        [Fact]
        public void Time_PulsesDailyForTheDuration()
        {
            SetUtcNow(V(2026, 11, 8, 6, 59));
            Configure(PulseAtType.Time, "07:00:00", duration: 5);
            Assert.Equal(new DateTime(2026, 11, 8, 7, 0, 0), NextLocal);
            Assert.Equal(60_000, SutTimer.Interval);

            SetUtcNow(V(2026, 11, 8, 7));
            FireTick();
            Assert.Equal(new object[] { true }, Values(F.Output));
            Assert.Equal(new DateTime(2026, 11, 9, 7, 0, 0), NextLocal);
            Assert.Equal(5000, SutTimer.Interval);

            SetUtcNow(V(2026, 11, 8, 7, 0, 5));
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.Output));
        }

        [Fact]
        public void Time_ZeroDuration_IsAShortPulse()
        {
            SetUtcNow(V(2026, 11, 8, 6, 59));
            Configure(PulseAtType.Time, "07:00", duration: 0);
            SetUtcNow(V(2026, 11, 8, 7));
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.Output));
        }

        [Fact]
        public void OneTime_PulsesOnlyOnTheDate()
        {
            SetUtcNow(V(2026, 11, 8, 12));
            Configure(PulseAtType.Time, "08:30:00", oneTime: true, date: "2026-12-24");
            Assert.Equal(new DateTime(2026, 12, 24, 8, 30, 0), NextLocal);

            SetUtcNow(V(2026, 12, 24, 8, 30));
            FireTick();
            Assert.Equal(true, Values(F.Output)[0]);
            Assert.Null(NextLocal);
        }

        [Fact]
        public void OneTime_PastDate_NeverPulses()
        {
            SetUtcNow(V(2026, 11, 8, 12));
            Configure(PulseAtType.Time, "08:30:00", oneTime: true, date: "2026-01-01");
            Assert.Null(NextLocal);
        }

        [Theory]
        [InlineData(PulseAtType.Minute, 2026, 11, 8, 12, 1, 0)]
        [InlineData(PulseAtType.Hour, 2026, 11, 8, 13, 0, 0)]
        [InlineData(PulseAtType.Day, 2026, 11, 9, 0, 0, 0)]
        [InlineData(PulseAtType.Week, 2026, 11, 9, 0, 0, 0)]
        [InlineData(PulseAtType.Month, 2026, 12, 1, 0, 0, 0)]
        [InlineData(PulseAtType.Year, 2027, 1, 1, 0, 0, 0)]
        public void Periodic_NextPulse(PulseAtType type, int year, int month, int day, int hour, int minute, int second)
        {
            SetUtcNow(V(2026, 11, 8, 12, 0, 30));
            Configure(type);
            Assert.Equal(new DateTime(year, month, day, hour, minute, second), NextLocal);
        }

        [Fact]
        public void Week_OnAMonday_IsTheNextMonday()
        {
            SetUtcNow(V(2026, 11, 9, 0, 0, 0));
            Configure(PulseAtType.Week);
            Assert.Equal(new DateTime(2026, 11, 16, 0, 0, 0), NextLocal);
        }

        [Fact]
        public void Hour_PulsesEveryHour()
        {
            SetUtcNow(V(2026, 11, 8, 12, 30));
            Configure(PulseAtType.Hour, duration: 0);
            SetUtcNow(V(2026, 11, 8, 13));
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.Output));
            Assert.Equal(new DateTime(2026, 11, 8, 14, 0, 0), NextLocal);
        }

        [Fact]
        public void Sunrise_UsesTheCoreLocation()
        {
            SetUtcNow(V(2026, 6, 21, 0, 30));
            OperatingModeState.Publish(Recorder, OperatingModeState.Json(new DateOnly(2026, 6, 21), latitude: 48.2082, longitude: 16.3738));
            Configure(PulseAtType.Sunrise, offset: -10);

            // Vienna, 21 June: sunrise about 04:53 CEST
            var next = NextLocal!.Value;
            Assert.Equal(new DateTime(2026, 6, 21), next.Date);
            Assert.InRange(next.TimeOfDay, new TimeSpan(4, 35, 0), new TimeSpan(4, 50, 0));
        }

        [Fact]
        public void Sunset_WithoutLocation_Waits()
        {
            SetUtcNow(V(2026, 6, 21, 12));
            Configure(PulseAtType.Sunset);
            Assert.Null(NextLocal);
            Assert.Equal(60_000, SutTimer.Interval);

            OperatingModeState.Publish(Recorder, OperatingModeState.Json(new DateOnly(2026, 6, 21)));
            FireTick();
            Assert.NotNull(NextLocal);
            Assert.InRange(NextLocal!.Value.TimeOfDay, new TimeSpan(20, 50, 0), new TimeSpan(21, 10, 0));
        }

        [Fact]
        public void Off_LocksTheBlock()
        {
            SetUtcNow(V(2026, 11, 8, 6, 59));
            Configure(PulseAtType.Time, "07:00:00", duration: 60);
            SetUtcNow(V(2026, 11, 8, 7));
            FireTick();
            Assert.Equal(new object[] { true }, Values(F.Output));

            Input(F.InputOff, true);
            Assert.Equal(new object[] { true, false }, Values(F.Output));
            Assert.Null(NextLocal);

            Input(F.InputOff, false);
            Assert.Equal(new DateTime(2026, 11, 9, 7, 0, 0), NextLocal);
        }

        [Fact]
        public void VisuTime_IsKeptAcrossRestarts()
        {
            SetUtcNow(V(2026, 11, 8, 12));
            Configure(PulseAtType.Time, "07:00:00");

            Sut.ValueChanged(Interface(F.ParamTime), new VisuDispatchable(Interface(F.ParamTime).ObjId), "18:15");
            Assert.Equal("18:15:00", Rule.TimeText);
            Assert.Equal(new DateTime(2026, 11, 8, 18, 15, 0), NextLocal);

            // invalid visu values are ignored
            Sut.ValueChanged(Interface(F.ParamTime), new VisuDispatchable(Interface(F.ParamTime).ObjId), "25:99");
            Assert.Equal("18:15:00", Rule.TimeText);

            Stop();
            var restarted = (PulseAtRule)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            restarted.Start().GetAwaiter().GetResult();
            try
            {
                Assert.Equal("18:15:00", restarted.TimeText);
                var data = (Dictionary<string, object>)restarted.GetDataForVisu();
                Assert.Equal("18:15:00", data[F.TimeKey]);
            }
            finally
            {
                restarted.Stop().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void Dst_DailyTimeKeepsTheWallClock()
        {
            // 2026-10-25 03:00 -> 02:00: 07:00 on the 25th is 06:00 UTC, on the 24th 05:00 UTC
            SetUtcNow(V(2026, 10, 24, 12));
            Configure(PulseAtType.Time, "07:00:00");
            Assert.Equal(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero), Rule.Next!.Value.ToUniversalTime());
        }

        [Fact]
        public void ParseDate_AcceptsIsoAndGerman()
        {
            Assert.Equal(new DateOnly(2026, 12, 24), PulseAtRule.ParseDate("2026-12-24"));
            Assert.Equal(new DateOnly(2026, 12, 24), PulseAtRule.ParseDate("24.12.2026"));
            Assert.Null(PulseAtRule.ParseDate(""));
            Assert.Null(PulseAtRule.ParseDate("xyz"));
        }
    }
}
