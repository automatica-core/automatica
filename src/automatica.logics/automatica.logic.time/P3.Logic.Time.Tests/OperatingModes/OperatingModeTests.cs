using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using P3.Logic.Time.OperatingModes;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Time.OperatingModes.OperatingModeLogicFactory;

namespace P3.Logic.Time.Tests.OperatingModes
{
    public class OperatingModeTests : TimeLogicTest<OperatingModeLogicFactory>
    {
        private static readonly DateOnly Today = new(2026, 11, 8);

        private void Configure(string mode)
        {
            SetUtcNow(new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc));
            Interface(F.ParamMode).Value = mode;
            Start();
        }

        private void Publish(params string[] active)
        {
            OperatingModeState.Publish(Recorder, OperatingModeState.Json(Today, new Dictionary<DateOnly, string[]> { [Today] = active }));
        }

        [Fact]
        public void NoCoreState_IsInactive()
        {
            Configure("vacation");
            Assert.Equal(new object[] { false }, Values(F.OutputActive));
            Assert.Equal("", Last(F.OutputModes));
            Assert.Equal(OperatingModeWatcher.PollMilliseconds, SutTimer.Interval);
        }

        [Fact]
        public void FollowsTheCoreState()
        {
            Publish("vacation", "school-holidays");
            Configure("vacation");
            Assert.Equal(new object[] { true }, Values(F.OutputActive));
            Assert.Equal("vacation,school-holidays", Last(F.OutputModes));

            Publish("public-holiday");
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.OutputActive));
            Assert.Equal("public-holiday", Last(F.OutputModes));

            // no change, nothing sent
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(F.OutputActive));
        }

        [Fact]
        public void Input_RequestsTheMode()
        {
            Configure("vacation");
            Input(F.InputOn, true);
            Input(F.InputOn, false);

            var requests = OperatingModeState.Requests(Recorder);
            Assert.Equal(2, requests.Count);
            Assert.Equal("vacation", JObject.Parse(requests[0])["mode"]!.ToString());
            Assert.True(JObject.Parse(requests[0])["state"]!.Value<bool>());
            Assert.Equal(JTokenType.Null, JObject.Parse(requests[1])["state"]!.Type);
            Assert.Equal(1000, SutTimer.Interval);
        }

        [Fact]
        public void Snapshot_Parse()
        {
            var snapshot = OperatingModeSnapshot.Parse(OperatingModeState.Json(Today, new Dictionary<DateOnly, string[]>
            {
                [Today] = new[] { "vacation" },
                [Today.AddDays(1)] = new[] { "public-holiday", "vacation" }
            }));

            Assert.True(snapshot.IsActive("Vacation"));
            Assert.Equal(new[] { "public-holiday", "vacation" }, snapshot.ActiveOn(Today.AddDays(1), Today));
            Assert.Empty(snapshot.ActiveOn(Today.AddDays(40), Today));
            Assert.True(snapshot.HasLocation);
            Assert.Equal("Vacation", snapshot.Names["vacation"]);

            Assert.True(OperatingModeSnapshot.Parse("{broken").IsEmpty);
            Assert.True(OperatingModeSnapshot.Parse(null).IsEmpty);
        }
    }
}
