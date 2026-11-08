using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.StringOperations.Tests.TestSupport;
using P3.Logic.StringOperations.Tracker;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Tracker
{
    public class TrackerTests : TextLogicTest<TrackerLogicFactory>
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "automatica-tracker-tests");

        static TrackerTests()
        {
            // before the base class starts its own instance
            TrackerRule.StorageDirectoryOverride = Path.Combine(Root, "base");
        }

        private readonly string _folder;

        public TrackerTests()
        {
            _folder = Path.Combine(Root, Guid.NewGuid().ToString("N"));
            TrackerRule.StorageDirectoryOverride = _folder;
        }

        public override void Dispose()
        {
            base.Dispose();
            try
            {
                Directory.Delete(_folder, true);
            }
            catch (Exception)
            {
                // not created
            }
        }

        private TrackerRule Tracker => (TrackerRule)Sut;

        private void Log(object value, string source = "Door") => Input(TrackerLogicFactory.InputValue, value, new NamedSource(source));

        [Fact]
        public void Entries_NewestFirst_WithTime()
        {
            Start();
            var time = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
            FakeTimeProvider.SetDateTime(time);
            Log("Patrik");
            FakeTimeProvider.SetDateTime(time.AddMinutes(5));
            Log("Anna");

            Assert.Equal(new[] { "Anna", "Patrik" }, Tracker.Entries.Select(a => a.Text).ToArray());
            Assert.Equal(time.AddMinutes(5), Tracker.Entries[0].Time.UtcDateTime);
            Assert.Equal("Anna", Last(TrackerLogicFactory.OutputLast));
            Assert.Equal(2, Last(TrackerLogicFactory.OutputCount));
        }

        [Fact]
        public void Template_ValueAndSourceName()
        {
            Param(TrackerLogicFactory.ParamTemplate, "<n> opened by <v>");
            Start();
            Log("Anna", "Front door");
            Assert.Equal("Front door opened by Anna", Tracker.Entries[0].Text);
            Assert.Equal("Front door", Tracker.Entries[0].Source);
        }

        [Fact]
        public void Bools_UseTheOnOffTexts()
        {
            Param(TrackerLogicFactory.ParamOnText, "<n> opened");
            Param(TrackerLogicFactory.ParamOffText, "<n> closed");
            Param(TrackerLogicFactory.ParamTemplate, "<n>: <v.1> °C");
            Start();

            Log(true, "Garage");
            Log(false, "Garage");
            Log(21.26, "Garage");

            Assert.Equal(new[] { "Garage: 21.3 °C", "Garage closed", "Garage opened" }, Tracker.Entries.Select(a => a.Text).ToArray());
        }

        [Fact]
        public void BoolsWithoutOnOffTexts_UseTheTemplate()
        {
            Start();
            Log(true);
            Assert.Equal("1", Tracker.Entries[0].Text);
        }

        [Fact]
        public void EmptyValues_AreNoEntries()
        {
            Start();
            Log("");
            Log(null);
            Assert.Empty(Tracker.Entries);
        }

        [Fact]
        public void KeepsTheLastEntries()
        {
            Param(TrackerLogicFactory.ParamEntries, 3);
            Start();
            for (var i = 1; i <= 5; i++)
            {
                Log($"#{i}");
            }
            Assert.Equal(new[] { "#5", "#4", "#3" }, Tracker.Entries.Select(a => a.Text).ToArray());
            Assert.Equal(3, Last(TrackerLogicFactory.OutputCount));

            ChangeParam(TrackerLogicFactory.ParamEntries, 2);
            Assert.Equal(2, Tracker.Entries.Count);
            Assert.Equal(2, Last(TrackerLogicFactory.OutputCount));
        }

        [Fact]
        public void EntryLimit_IsClamped()
        {
            Param(TrackerLogicFactory.ParamEntries, 0);
            Start();
            Log("a");
            Log("b");
            Log("c");
            Assert.Equal(TrackerLogicFactory.MinEntries, Tracker.Entries.Count);
        }

        [Fact]
        public void Reset_OnRisingEdge()
        {
            Start();
            Log("a");
            Log("b");

            Input(TrackerLogicFactory.InputReset, true);
            Assert.Empty(Tracker.Entries);
            Assert.Equal(0, Last(TrackerLogicFactory.OutputCount));
            Assert.Equal("", Last(TrackerLogicFactory.OutputLast));

            Log("c");
            // still 1, no edge
            Assert.Empty(Input(TrackerLogicFactory.InputReset, true));
            Assert.Single(Tracker.Entries);
        }

        [Fact]
        public void Remanent_EntriesSurviveARestart()
        {
            Start();
            Log("a");
            Log("b");
            Assert.True(File.Exists(Tracker.StoragePath));
            Stop();

            // a new instance of the same logic (core restart)
            var restarted = (TrackerRule)Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, Recorder));
            restarted.Start().GetAwaiter().GetResult();
            try
            {
                Assert.Equal(new[] { "b", "a" }, restarted.Entries.Select(a => a.Text).ToArray());
                // the outputs get the stored state
                Assert.Equal("b", Last(TrackerLogicFactory.OutputLast));
                Assert.Equal(2, Last(TrackerLogicFactory.OutputCount));
            }
            finally
            {
                restarted.Stop().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void NotRemanent_NothingIsStored()
        {
            Param(TrackerLogicFactory.ParamRemanent, false);
            Start();
            Log("a");
            Assert.False(File.Exists(Tracker.StoragePath));
        }

        [Fact]
        public void SwitchingRemanentOff_DeletesTheFile()
        {
            Start();
            Log("a");
            Assert.True(File.Exists(Tracker.StoragePath));
            ChangeParam(TrackerLogicFactory.ParamRemanent, false);
            Assert.False(File.Exists(Tracker.StoragePath));
        }

        [Fact]
        public void BrokenFile_StartsEmpty()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Tracker.StoragePath, "{ not json");
            Start();
            Assert.Empty(Tracker.Entries);
            Log("a");
            Assert.Single(Tracker.Entries);
        }

        [Fact]
        public void VisuData_HasTheEntries()
        {
            Start();
            Log("a", "Door");
            var entries = Data<List<Dictionary<string, object>>>("entries");
            Assert.Single(entries);
            Assert.Equal("a", entries[0]["text"]);
            Assert.Equal("Door", entries[0]["source"]);
            Assert.Equal(TrackerLogicFactory.DefaultEntries, Data<int>("limit"));
        }
    }
}
