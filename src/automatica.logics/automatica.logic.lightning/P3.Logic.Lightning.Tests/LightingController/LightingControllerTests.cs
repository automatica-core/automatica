using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.LightingController;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Lightning.Tests.LightingController
{
    public class LightingControllerTests : LightingLogicTest<LightingControllerLogicFactory>
    {
        private static readonly LightingControllerIds Ids = LightingControllerLogicFactory.Ids;

        private LightingControllerLogic Controller => (LightingControllerLogic)Sut;

        private static Guid Circuit(int number) => Ids.CircuitOutputs[number - 1];

        /// <summary>
        /// circuit 1 dimmer "Ceiling", 2 switch, 3 RGB, 4 tunable white; moods 1 Reading, 2 Cosy
        /// </summary>
        private void Configure(string moods = "Reading:1=100,2=on;Cosy:1=30,3=#FF8800,4=40@2700")
        {
            Param(Ids.CircuitTypes[0], (int)CircuitType.Dimmer);
            Param(Ids.CircuitNames[0], "Ceiling");
            Param(Ids.CircuitTypes[1], (int)CircuitType.Switch);
            Param(Ids.CircuitTypes[2], (int)CircuitType.Rgb);
            Param(Ids.CircuitTypes[3], (int)CircuitType.TunableWhite);
            Param(Ids.Moods, moods);
            Start();
        }

        private void Command(string command) => Input(Ids.Command, command);

        private JObject VisuState() => JObject.Parse((string)Last(Ids.VisuState));

        [Fact]
        public void Start_SwitchesNothing_AndPublishesTheState()
        {
            Configure();

            Assert.Empty(Values(Circuit(1)));
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
            Assert.Equal(false, Last(Ids.AnyOn));
            var state = VisuState();
            Assert.Equal(0, (int)state["mood"]);
            Assert.Equal(new[] { 1, 2 }, state["moods"]!.Select(m => (int)m["id"]).ToArray());
            Assert.Equal(4, state["circuits"]!.Count());
            Assert.Equal("Ceiling", (string)state["circuits"]![0]!["name"]);
        }

        [Fact]
        public void Mood_SetsEveryCircuitInItsFormat()
        {
            Configure();

            Input(Ids.MoodSelect, 2);

            Assert.Equal(30, Last(Circuit(1)));
            Assert.Equal(false, Last(Circuit(2)));
            Assert.Equal("#FF8800", Last(Circuit(3)));
            Assert.Equal(40, Last(Circuit(4)));
            Assert.Equal(2700, Last(Ids.KelvinOutputs[3]));
            Assert.Equal(2, Last(Ids.ActiveMood));
            Assert.Equal(true, Last(Ids.AnyOn));
        }

        [Fact]
        public void UnusedCircuits_GetNoOutput()
        {
            Configure();

            Input(Ids.MoodSelect, 99);

            Assert.Empty(Values(Circuit(5)));
            Assert.Empty(Values(Ids.KelvinOutputs[0]));
        }

        [Fact]
        public void AllOn_And_AllOff()
        {
            Configure();

            Pulse(Ids.On);
            Assert.Equal(100, Last(Circuit(1)));
            Assert.Equal(true, Last(Circuit(2)));
            Assert.Equal("#FFFFFF", Last(Circuit(3)));
            Assert.Equal(Mood.AllOn, Last(Ids.ActiveMood));

            Pulse(Ids.Off);
            Assert.Equal(0, Last(Circuit(1)));
            Assert.Equal(false, Last(Circuit(2)));
            Assert.Equal("#000000", Last(Circuit(3)));
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
            Assert.Equal(false, Last(Ids.AnyOn));
        }

        [Fact]
        public void AllOn_KeepsTheColorOfTheRgbCircuit()
        {
            Configure();
            Input(Ids.MoodSelect, 2);

            Input(Ids.MoodSelect, 99);

            Assert.Equal("#FF8800", Last(Circuit(3)));
            Assert.Equal(2700, Last(Ids.KelvinOutputs[3]));
        }

        [Fact]
        public void MoodPlus_CyclesUserMoodsAndAllOn_FromOffStartsWithTheFirst()
        {
            Configure();

            var moods = new[] { 1, 2, 99, 1 }.Select(_ =>
            {
                Pulse(Ids.MoodPlus);
                return (int)Last(Ids.ActiveMood);
            }).ToArray();

            Assert.Equal(new[] { 1, 2, 99, 1 }, moods);
        }

        [Fact]
        public void MoodMinus_FromOffStartsWithAllOn_ThenGoesBack()
        {
            Configure();

            Pulse(Ids.MoodMinus);
            Assert.Equal(99, Last(Ids.ActiveMood));
            Pulse(Ids.MoodMinus);
            Assert.Equal(2, Last(Ids.ActiveMood));
        }

        [Fact]
        public void PulseInputs_ReactOnTheRisingEdgeOnly()
        {
            Configure();

            Input(Ids.MoodPlus, true);
            Input(Ids.MoodPlus, true);
            Assert.Equal(1, Last(Ids.ActiveMood));

            Input(Ids.MoodPlus, false);
            Input(Ids.MoodPlus, 1);
            Assert.Equal(2, Last(Ids.ActiveMood));
        }

        [Fact]
        public void UnknownMood_IsIgnored()
        {
            Configure();
            Input(Ids.MoodSelect, 1);

            Input(Ids.MoodSelect, 55);

            Assert.Equal(1, Last(Ids.ActiveMood));
        }

        [Fact]
        public void MoodSelectZero_SwitchesOff()
        {
            Configure();
            Input(Ids.MoodSelect, 1);

            Input(Ids.MoodSelect, 0);

            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
        }

        [Fact]
        public void SetCircuit_MakesTheMoodManual()
        {
            Configure();
            Input(Ids.MoodSelect, 1);

            Command("set:1:55");

            Assert.Equal(55, Last(Circuit(1)));
            Assert.Equal(Mood.Manual, Last(Ids.ActiveMood));

            Command("set:3:#004080");
            Assert.Equal("#004080", Last(Circuit(3)));

            Command("set:4:80@5000");
            Assert.Equal(80, Last(Circuit(4)));
            Assert.Equal(5000, Last(Ids.KelvinOutputs[3]));

            Command("set:2:off");
            Assert.Equal(false, Last(Circuit(2)));
        }

        [Fact]
        public void SetCircuit_LastLightOff_IsAllOff()
        {
            Configure();
            Command("set:1:50");

            Command("set:1:0");

            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
            Assert.Equal(false, Last(Ids.AnyOn));
        }

        [Theory]
        [InlineData("set:9:50")]
        [InlineData("set:5:50")]
        [InlineData("set:1:abc")]
        [InlineData("dance")]
        [InlineData("")]
        public void BadCommands_ChangeNothing(string command)
        {
            Configure();
            ClearRecorded();

            Command(command);

            Assert.Empty(Values(Circuit(1)));
            Assert.Empty(Values(Ids.ActiveMood));
        }

        [Fact]
        public void SaveNewMood_StoresTheCurrentLights_AndSurvivesARestart()
        {
            Configure("");
            Command("set:1:42");
            Command("set:3:#00FF00");

            Command("save:new:Evening");

            var state = VisuState();
            Assert.Equal(1, (int)state["mood"]);
            Assert.Equal("Evening", (string)state["moods"]![0]!["name"]);

            Restart();
            Command("off");
            Command("mood:1");

            Assert.Equal(42, Last(Circuit(1)));
            Assert.Equal("#00FF00", Last(Circuit(3)));
            Assert.Equal(false, Last(Circuit(2)));
        }

        [Fact]
        public void SavedMoods_WinOverTheParameter()
        {
            Configure();
            Command("set:1:10");
            Command("save:new:Night");

            SetParam(Ids.Moods, "Other:1=100");

            Assert.Equal(new[] { "Reading", "Cosy", "Night" }, Controller.MoodList.Select(a => a.Name).ToArray());
        }

        [Fact]
        public void MoodParameter_ChangedAtRuntime_IsUsedWhileNothingWasSaved()
        {
            Configure();

            SetParam(Ids.Moods, "Bright:1=90");

            Input(Ids.MoodSelect, 1);
            Assert.Equal(90, Last(Circuit(1)));
        }

        [Fact]
        public void SaveExistingMood_OverwritesIt_RenameAndDelete()
        {
            Configure();
            Command("set:1:5");

            Command("save:2:");
            Assert.Equal("Cosy", Controller.MoodList.Single(a => a.Id == 2).Name);
            Command("off");
            Command("mood:2");
            Assert.Equal(5, Last(Circuit(1)));
            Assert.Equal(false, Last(Circuit(2)));

            Command("rename:2:Night light");
            Assert.Equal("Night light", Controller.MoodList.Single(a => a.Id == 2).Name);

            Command("delete:2");
            Assert.DoesNotContain(Controller.MoodList, a => a.Id == 2);
            Assert.Equal(Mood.Manual, Last(Ids.ActiveMood));
        }

        [Theory]
        [InlineData("save:99:x")]
        [InlineData("save:0:x")]
        [InlineData("save:90:x")]
        public void Save_BuiltInOrOutOfRangeIds_AreRejectedOrCreateANewMood(string command)
        {
            Configure("");
            Command("set:1:50");

            Command(command);

            Assert.All(Controller.MoodList, a => Assert.InRange(a.Id, 1, Mood.MaxUserMood));
        }

        [Fact]
        public void State_SurvivesARestart_WithoutSwitching()
        {
            Configure();
            Input(Ids.MoodSelect, 2);
            ClearRecorded();

            Restart();

            Assert.Empty(Values(Circuit(1)));
            Assert.Equal(2, Controller.CurrentMood);
            Assert.Equal(2, (int)VisuState()["mood"]);
            Pulse(Ids.MoodPlus);
            Assert.Equal(99, Last(Ids.ActiveMood));
        }

        // --- motion ---

        [Fact]
        public void Motion_SwitchesTheMotionMoodOn_AndOffAfterTheOverrun()
        {
            Param(Ids.MotionMood, 1);
            Param(Ids.MotionOffDelay, 60);
            Configure();

            Input(Ids.Motion, true);
            Assert.Equal(1, Last(Ids.ActiveMood));

            AdvanceSeconds(600);
            Assert.Equal(1, Last(Ids.ActiveMood));

            Input(Ids.Motion, false);
            AdvanceSeconds(59);
            Assert.Equal(1, Last(Ids.ActiveMood));
            AdvanceSeconds(1);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
        }

        [Fact]
        public void Motion_Again_CancelsTheOverrun()
        {
            Param(Ids.MotionOffDelay, 60);
            Configure();
            Input(Ids.Motion, true);
            Input(Ids.Motion, false);
            AdvanceSeconds(50);

            Input(Ids.Motion, true);
            AdvanceSeconds(120);

            Assert.Equal(99, Last(Ids.ActiveMood));
            Assert.Null(Controller.MotionOffAt);
        }

        [Fact]
        public void Motion_WhenBright_DoesNotSwitchOn()
        {
            Param(Ids.BrightnessThreshold, 30.0);
            Configure();

            Input(Ids.Brightness, 100);
            Input(Ids.Motion, true);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            Input(Ids.Motion, false);
            Input(Ids.Brightness, 10);
            Input(Ids.Motion, true);
            Assert.Equal(99, Last(Ids.ActiveMood));
        }

        [Fact]
        public void Motion_ThresholdZero_IgnoresTheBrightness()
        {
            Param(Ids.BrightnessThreshold, 0.0);
            Configure();

            Input(Ids.Brightness, 5000);
            Input(Ids.Motion, true);

            Assert.Equal(99, Last(Ids.ActiveMood));
        }

        [Fact]
        public void Motion_LightsAlreadyOn_KeepsTheMood()
        {
            Configure();
            Input(Ids.MoodSelect, 2);

            Input(Ids.Motion, true);

            Assert.Equal(2, Last(Ids.ActiveMood));
        }

        [Fact]
        public void OperatedByHand_AfterMotion_UsesTheManualOverrun()
        {
            Param(Ids.MotionOffDelay, 60);
            Param(Ids.ManualOffDelay, 600);
            Configure();
            Input(Ids.Motion, true);

            Input(Ids.MoodSelect, 2);
            Input(Ids.Motion, false);

            AdvanceSeconds(120);
            Assert.Equal(2, Last(Ids.ActiveMood));
            AdvanceSeconds(480);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
        }

        [Fact]
        public void ManualOffDelayZero_NeverSwitchesOffAfterManualUse()
        {
            Param(Ids.ManualOffDelay, 0.0);
            Configure();
            Input(Ids.MoodSelect, 1);

            Input(Ids.Motion, true);
            Input(Ids.Motion, false);
            AdvanceSeconds(100000);

            Assert.Equal(1, Last(Ids.ActiveMood));
        }

        [Fact]
        public void ManualOffDelay_ShorterThanMotionOverrun_IsUsed()
        {
            Param(Ids.MotionOffDelay, 900);
            Param(Ids.ManualOffDelay, 120);
            Configure();
            Input(Ids.Motion, true);
            Input(Ids.Motion, false);

            AdvanceSeconds(120);

            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
        }

        [Fact]
        public void SwitchedOffByHand_MotionIsIgnored_UntilNoMotionForTheTimeout()
        {
            Param(Ids.AutomaticTimeout, 300);
            Configure();
            Input(Ids.Motion, true);
            Pulse(Ids.Off);
            Assert.True(Controller.MotionSuspended);

            Input(Ids.Motion, false);
            AdvanceSeconds(200);
            Input(Ids.Motion, true);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            // the timeout restarts when the motion ends
            Input(Ids.Motion, false);
            AdvanceSeconds(299);
            Assert.True(Controller.MotionSuspended);
            AdvanceSeconds(1);
            Assert.False(Controller.MotionSuspended);

            Input(Ids.Motion, true);
            Assert.Equal(99, Last(Ids.ActiveMood));
        }

        [Fact]
        public void DisableMotion_BlocksMotion_AndStartsTheOverrunOfMotionLights()
        {
            Param(Ids.MotionOffDelay, 60);
            Configure();
            Input(Ids.Motion, true);

            Input(Ids.DisableMotion, true);
            AdvanceSeconds(60);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            Input(Ids.Motion, false);
            Input(Ids.Motion, true);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            Input(Ids.DisableMotion, false);
            Input(Ids.Motion, false);
            Input(Ids.Motion, true);
            Assert.Equal(99, Last(Ids.ActiveMood));
        }

        // --- presence simulation ---

        [Fact]
        public void PresenceSimulation_SwitchesTheMotionMoodInTheWindow()
        {
            Param(Ids.MotionMood, 1);
            Param(Ids.PresenceSimulation.MinActivations, 1);
            Param(Ids.PresenceSimulation.MaxActivations, 1);
            Configure();
            Controller.PresenceSimulation.Simulation.UseRandom(new Random(11));

            Input(Ids.PresenceSimulation.Enable, true);
            Assert.Equal(Monday.AddHours(18), Controller.NextTick);

            AdvanceSeconds(6 * 3600);
            var plan = Controller.PresenceSimulation.Simulation.Plan;
            Assert.Single(plan);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            Advance(plan[0].Start - Now + TimeSpan.FromSeconds(1));
            Assert.Equal(1, Last(Ids.ActiveMood));
            Assert.True((bool)VisuState()["simulation"]);

            Advance(plan[0].End - Now);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));

            AdvanceSeconds(6 * 3600);
            Assert.Equal(Mood.AllOff, Last(Ids.ActiveMood));
            Assert.False(Controller.PresenceSimulation.IsSimulating);
        }

        [Fact]
        public void PresenceSimulation_DoesNotRecordItsOwnChanges()
        {
            Configure();
            Input(Ids.MoodSelect, 1);
            Input(Ids.MoodSelect, 0);
            var recorded = Controller.PresenceSimulation.Simulation.Events.Count;

            Input(Ids.PresenceSimulation.Enable, true);
            AdvanceSeconds(12 * 3600);

            Assert.Equal(recorded, Controller.PresenceSimulation.Simulation.Events.Count);
        }

        [Fact]
        public void PresenceSimulation_Recording_SurvivesARestart()
        {
            Configure();
            Input(Ids.MoodSelect, 1);
            Input(Ids.MoodSelect, 0);

            Restart();

            Assert.Equal(2, Controller.PresenceSimulation.Simulation.Events.Count);
        }

        [Fact]
        public void Stop_ClearsTheDeadline_AndInputsAreIgnored()
        {
            Configure();
            Input(Ids.Motion, true);
            Input(Ids.Motion, false);
            Assert.NotNull(Controller.NextTick);

            Stop();
            ClearRecorded();
            Input(Ids.MoodSelect, 1);

            Assert.Null(Controller.NextTick);
            Assert.Empty(Values(Ids.ActiveMood));
        }

        [Fact]
        public void Factory_HasTheDocumentedDefaults()
        {
            Assert.Equal(LightingControllerLogicFactory.DefaultMotionOffDelay, GetLogicInterfaceByTemplate(Ids.MotionOffDelay).Value);
            Assert.Equal(LightingControllerLogicFactory.DefaultManualOffDelay, GetLogicInterfaceByTemplate(Ids.ManualOffDelay).Value);
            Assert.Equal(30.0, GetLogicInterfaceByTemplate(Ids.BrightnessThreshold).Value);
            Assert.Equal(Mood.AllOn, Convert.ToInt32(GetLogicInterfaceByTemplate(Ids.MotionMood).Value));
            Assert.Equal((int)CircuitType.Dimmer, Convert.ToInt32(GetLogicInterfaceByTemplate(Ids.CircuitTypes[0]).Value));
            Assert.Equal((int)CircuitType.Unused, Convert.ToInt32(GetLogicInterfaceByTemplate(Ids.CircuitTypes[1]).Value));
            Assert.NotNull(GetLogicInterfaceByTemplate(Ids.PresenceSimulation.Window));

            var all = RuleInstance.RuleInterfaceInstance.Select(a => a.This2RuleInterfaceTemplate).ToList();
            Assert.Equal(all.Count, all.Distinct().Count());
        }

        [Fact]
        public void TemplateIds_AreStableAndUnique()
        {
            Assert.Equal(TemplateIds.Of("P3.Logic.Lightning.LightingController.CircuitOutput1"), Ids.CircuitOutputs[0]);
            var hotel = LightingControllerIds.Derive("P3.Logic.Lightning.HotelLightingController");
            Assert.NotEqual(hotel.MoodPlus, Ids.MoodPlus);
        }
    }
}
