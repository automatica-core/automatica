using System;
using System.Linq;
using P3.Logic.Operations.Garage;
using Xunit;
using F = P3.Logic.Operations.Garage.GarageGateLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class GarageGateTests : BlockTest<GarageGateLogicFactory>
    {
        private void Configure(double openTime = 20, double closeTime = 20, int motorMode = 0, int autoClose = 0, int partial = 20)
        {
            Param(F.ParamOpenTime, openTime);
            Param(F.ParamCloseTime, closeTime);
            Param(F.ParamMotorMode, motorMode.ToString());
            Param(F.ParamAutoClose, autoClose);
            Param(F.ParamPartialPosition, partial);
            Start();
        }

        private int State => Convert.ToInt32(Last(F.OutputState));
        private double Position => Convert.ToDouble(VisuData["position"]);
        private System.Timers.Timer Travel => Timer();

        [Fact]
        public void Factory_HasAllInterfacesAndDefaults()
        {
            Assert.Equal(13 + 7 + 7, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(20.0, Interface(F.ParamOpenTime).Value);
            Assert.Equal("0", Interface(F.ParamType).Value);
            Assert.Equal(0L, Interface(F.ParamAutoClose).ValueInteger);
        }

        [Fact]
        public void Start_ReportsClosed()
        {
            Configure();
            Assert.Equal((int)GarageState.Closed, State);
            Assert.Equal(0.0, Last(F.OutputPosition));
            Assert.Equal(false, Last(F.OutputIsOpen));
            Assert.Equal(false, Last(F.OutputLocked));
        }

        [Fact]
        public void Open_RunsTheOpenRelayForTheTravelTime()
        {
            Configure(openTime: 20);
            Input(F.InputOpen, true);

            Assert.Equal(true, Last(F.OutputOpen));
            Assert.NotEqual(true, Last(F.OutputClose));
            Assert.Equal((int)GarageState.Opening, State);
            Assert.True(Travel.Enabled);
            Assert.Equal(20000, Travel.Interval);

            Clock.Advance(10);
            Assert.Equal(50.0, Position);

            Clock.Advance(10);
            Fire();
            Assert.Equal(false, Last(F.OutputOpen));
            Assert.Equal((int)GarageState.Open, State);
            Assert.Equal(100.0, Last(F.OutputPosition));
            Assert.Equal(true, Last(F.OutputIsOpen));
        }

        [Fact]
        public void Open_WhileMoving_Stops()
        {
            Configure(openTime: 20);
            Input(F.InputOpen, true);
            Clock.Advance(5);
            Input(F.InputOpen, true);

            Assert.Equal(false, Last(F.OutputOpen));
            Assert.Equal((int)GarageState.PartiallyOpen, State);
            Assert.Equal(25.0, Last(F.OutputPosition));
            Assert.False(Travel.Enabled);
        }

        [Fact]
        public void FalseValues_AreIgnored()
        {
            Configure();
            Input(F.InputOpen, false);
            Input(F.InputToggle, 0);
            Assert.Empty(Values(F.OutputOpen));
            Assert.Equal((int)GarageState.Closed, State);
        }

        [Fact]
        public void Toggle_CyclesOpenStopCloseStop()
        {
            Configure(openTime: 20, closeTime: 20);

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.Opening, State);
            Clock.Advance(10);

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.PartiallyOpen, State);

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.Closing, State);
            Assert.Equal(true, Last(F.OutputClose));
            Assert.Equal(10000, Travel.Interval);
            Clock.Advance(4);

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.PartiallyOpen, State);
            Assert.Equal(30.0, Last(F.OutputPosition));

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.Opening, State);
        }

        [Fact]
        public void Toggle_WhenFullyOpen_Closes()
        {
            Configure();
            Input(F.InputEndOpen, true);
            Assert.Equal((int)GarageState.Open, State);

            Input(F.InputToggle, true);
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void CompleteClose_WhileOpening_Reverses()
        {
            Configure(openTime: 20, closeTime: 40);
            Input(F.InputOpen, true);
            Clock.Advance(5);

            Input(F.InputCompleteClose, true);
            Assert.Equal(false, Last(F.OutputOpen));
            Assert.Equal(true, Last(F.OutputClose));
            Assert.Equal((int)GarageState.Closing, State);
            // 25 % at 40 s for the whole way
            Assert.Equal(10000, Travel.Interval);

            // complete close cannot be stopped by pressing it again
            Input(F.InputCompleteClose, true);
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void Relays_AreNeverOnTogether()
        {
            Configure();
            Input(F.InputCompleteOpen, true);
            Clock.Advance(3);
            Input(F.InputCompleteClose, true);
            Clock.Advance(1);
            Input(F.InputCompleteOpen, true);

            bool open = false, close = false;
            foreach (var (id, value) in Recorder.All())
            {
                if (id == F.OutputOpen)
                {
                    open = (bool)value;
                }
                else if (id == F.OutputClose)
                {
                    close = (bool)value;
                }
                Assert.False(open && close, "open and close relay on at the same time");
            }
        }

        [Fact]
        public void PartialOpen_StopsAtThePartialPosition()
        {
            Configure(openTime: 20, partial: 20);
            Input(F.InputPartialOpen, true);
            Assert.Equal(4000, Travel.Interval);

            Clock.Advance(4);
            Fire();
            Assert.Equal(false, Last(F.OutputOpen));
            Assert.Equal((int)GarageState.PartiallyOpen, State);
            Assert.Equal(20.0, Last(F.OutputPosition));

            // already there: nothing moves
            Input(F.InputPartialOpen, true);
            Assert.Equal((int)GarageState.PartiallyOpen, State);
        }

        [Fact]
        public void EndSwitch_StopsEarlyAndCorrectsThePosition()
        {
            Configure(openTime: 20);
            Input(F.InputOpen, true);
            Clock.Advance(15);
            Input(F.InputEndOpen, true);

            Assert.Equal(false, Last(F.OutputOpen));
            Assert.Equal((int)GarageState.Open, State);
            Assert.Equal(100.0, Last(F.OutputPosition));
            Assert.False(Travel.Enabled);

            Input(F.InputCompleteClose, true);
            Clock.Advance(2);
            Input(F.InputEndClosed, true);
            Assert.Equal((int)GarageState.Closed, State);
            Assert.Equal(false, Last(F.OutputClose));
        }

        [Fact]
        public void Obstacle_WhileClosing_OpensAgain_AndBlocksClosing()
        {
            Configure();
            Input(F.InputEndOpen, true);
            Input(F.InputClose, true);
            Clock.Advance(5);

            Input(F.InputObstacle, true);
            Assert.Equal(false, Last(F.OutputClose));
            Assert.Equal(true, Last(F.OutputOpen));
            Assert.Equal((int)GarageState.Opening, State);

            Clock.Advance(20);
            Fire();
            Input(F.InputCompleteClose, true);
            Assert.Equal((int)GarageState.Open, State);

            Input(F.InputObstacle, false);
            Input(F.InputCompleteClose, true);
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void Lock_StopsAndBlocksEverything()
        {
            Configure();
            Input(F.InputOpen, true);
            Input(F.InputLock, true);

            Assert.Equal(true, Last(F.OutputLocked));
            Assert.Equal(false, Last(F.OutputOpen));
            Input(F.InputOpen, true);
            VisuInput(F.InputOpen, true);
            Input(F.InputCentral, (int)GarageCommand.Open);
            Assert.NotEqual((int)GarageState.Opening, State);

            Input(F.InputLock, false);
            Assert.Equal(false, Last(F.OutputLocked));
            Input(F.InputOpen, true);
            Assert.Equal((int)GarageState.Opening, State);
        }

        [Fact]
        public void DisablePeriphery_BlocksLinksButNotVisuAndCentral()
        {
            Configure();
            Input(F.InputDisablePeriphery, true);

            Input(F.InputOpen, true);
            Assert.Equal((int)GarageState.Closed, State);

            VisuInput(F.InputOpen, true);
            Assert.Equal((int)GarageState.Opening, State);

            Input(F.InputCentral, (int)GarageCommand.Stop);
            Assert.Equal((int)GarageState.Closed, State);
            Input(F.InputCentral, (int)GarageCommand.PartialOpen);
            Assert.Equal((int)GarageState.Opening, State);
            Assert.Equal(20.0, VisuData["target"]);
        }

        [Theory]
        [InlineData(GarageCommand.Open, GarageState.Opening)]
        [InlineData(GarageCommand.Close, GarageState.Closed)]
        [InlineData(GarageCommand.PartialOpen, GarageState.Opening)]
        [InlineData(GarageCommand.None, GarageState.Closed)]
        public void CentralCommand_FromClosed(GarageCommand command, GarageState expected)
        {
            Configure();
            Input(F.InputCentral, (int)command);
            Assert.Equal((int)expected, State);
        }

        [Fact]
        public void SingleButton_OnePulseOpensFromClosed()
        {
            Configure(motorMode: 1);
            Param(F.ParamPulseLength, 300);
            ChangeParam(F.ParamPulseLength, 300);
            Input(F.InputOpen, true);

            Assert.Equal(new object[] { true }, Values(F.OutputPulse));
            Assert.Empty(Values(F.OutputOpen));
            Assert.Equal(300, Timer("_pulseTimer").Interval);

            Fire("_pulseTimer_Elapsed");
            Assert.Equal(new object[] { true, false }, Values(F.OutputPulse));
            Assert.False(Timer("_pulseTimer").Enabled);
        }

        [Fact]
        public void SingleButton_ReverseNeedsStopAndStart()
        {
            Configure(motorMode: 1);
            Input(F.InputOpen, true);
            Fire("_pulseTimer_Elapsed");
            Clock.Advance(5);

            Input(F.InputCompleteClose, true);
            for (var i = 0; i < 4; i++)
            {
                Fire("_pulseTimer_Elapsed");
            }
            // open: 1 pulse, reverse: stop + close = 2 pulses
            Assert.Equal(new object[] { true, false, true, false, true, false }, Values(F.OutputPulse));
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void SingleButton_DriveStopsByItselfAtTheEnd()
        {
            Configure(motorMode: 1, openTime: 10);
            Input(F.InputOpen, true);
            Fire("_pulseTimer_Elapsed");
            Clock.Advance(10);
            Fire();

            // no stop pulse at the end, the next pulse closes
            Assert.Equal(new object[] { true, false }, Values(F.OutputPulse));
            Assert.Equal((int)GarageState.Open, State);

            Input(F.InputClose, true);
            Fire("_pulseTimer_Elapsed");
            Assert.Equal(new object[] { true, false, true, false }, Values(F.OutputPulse));
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void SingleButton_SameDirectionAfterStop_NeedsThreePulses()
        {
            Configure(motorMode: 1);
            Input(F.InputOpen, true);
            Fire("_pulseTimer_Elapsed");
            Clock.Advance(5);
            Input(F.InputStop, true);
            Fire("_pulseTimer_Elapsed");
            ClearRecorded();

            // the drive would close next: close, stop, open
            Input(F.InputOpen, true);
            for (var i = 0; i < 6; i++)
            {
                Fire("_pulseTimer_Elapsed");
            }
            Assert.Equal(3, Values(F.OutputPulse).Count(a => Equals(a, true)));
            Assert.Equal((int)GarageState.Opening, State);
        }

        [Fact]
        public void SingleButton_PartialPositionSendsAStopPulse()
        {
            Configure(motorMode: 1, partial: 50, openTime: 10);
            Input(F.InputPartialOpen, true);
            Fire("_pulseTimer_Elapsed");
            Clock.Advance(5);
            Fire();
            Fire("_pulseTimer_Elapsed");

            Assert.Equal(new object[] { true, false, true, false }, Values(F.OutputPulse));
            Assert.Equal((int)GarageState.PartiallyOpen, State);
        }

        [Fact]
        public void AutoClose_ClosesAfterTheTimeWhenOpen()
        {
            Configure(openTime: 10, autoClose: 60);
            Input(F.InputOpen, true);
            Clock.Advance(10);
            Fire();

            var autoClose = Timer("_autoCloseTimer");
            Assert.True(autoClose.Enabled);
            Assert.Equal(60000, autoClose.Interval);

            Fire("_autoCloseTimer_Elapsed");
            Assert.Equal((int)GarageState.Closing, State);
        }

        [Fact]
        public void AutoClose_IsCancelledByAnyMove()
        {
            Configure(openTime: 10, autoClose: 60);
            Input(F.InputEndOpen, true);
            Assert.True(Timer("_autoCloseTimer").Enabled);

            Input(F.InputPartialOpen, true);
            Assert.False(Timer("_autoCloseTimer").Enabled);
        }

        [Fact]
        public void TravelTimeParameters_ApplyToTheNextMove()
        {
            Configure(openTime: 20);
            ChangeParam(F.ParamOpenTime, 8.5);
            Input(F.InputOpen, true);
            Assert.Equal(8500, Travel.Interval);
        }

        [Fact]
        public void VisuData_HasTypeTravelTimesAndPosition()
        {
            Param(F.ParamType, "2");
            Configure(openTime: 12, closeTime: 14);
            Input(F.InputOpen, true);
            Clock.Advance(3);

            var data = VisuData;
            Assert.Equal(2, data["gateType"]);
            Assert.Equal(12.0, data["openSeconds"]);
            Assert.Equal(14.0, data["closeSeconds"]);
            Assert.Equal(25.0, data["position"]);
            Assert.Equal(1, data["motion"]);
            Assert.Equal((int)GarageState.Opening, data["state"]);
            Assert.Equal(true, data[$"{F.OutputOpen}"]);
        }
    }

    public class GarageCentralTests : BlockTest<GarageCentralLogicFactory>
    {
        private object Command => Last(GarageCentralLogicFactory.OutputCommand);

        [Fact]
        public void Factory_HasEightStateInputs()
        {
            Assert.Equal(8, GarageCentralLogicFactory.InputStates.Length);
            Assert.Equal(7 + 8 + 3 + 1, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void Commands_AreSentEveryTime()
        {
            Start();
            Input(GarageCentralLogicFactory.InputCompleteOpen, true);
            Input(GarageCentralLogicFactory.InputCompleteOpen, true);
            Input(GarageCentralLogicFactory.InputCompleteClose, true);
            Input(GarageCentralLogicFactory.InputPartialOpen, true);
            Input(GarageCentralLogicFactory.InputStop, true);
            Input(GarageCentralLogicFactory.InputStop, false);

            Assert.Equal(new object[] { 1, 1, 2, 4, 3 }, Values(GarageCentralLogicFactory.OutputCommand));
        }

        [Fact]
        public void Toggle_StopsWhenMoving_ClosesWhenOpen_OpensWhenAllClosed()
        {
            Start();
            Input(GarageCentralLogicFactory.InputStates[0], (int)GarageState.Closed);
            Input(GarageCentralLogicFactory.InputStates[1], (int)GarageState.Closed);
            Input(GarageCentralLogicFactory.InputToggle, true);
            Assert.Equal((int)GarageCommand.Open, Command);

            Input(GarageCentralLogicFactory.InputStates[1], (int)GarageState.Opening);
            Input(GarageCentralLogicFactory.InputToggle, true);
            Assert.Equal((int)GarageCommand.Stop, Command);

            Input(GarageCentralLogicFactory.InputStates[1], (int)GarageState.PartiallyOpen);
            Input(GarageCentralLogicFactory.InputToggle, true);
            Assert.Equal((int)GarageCommand.Close, Command);
        }

        [Fact]
        public void Counts_OpenAndClosedDoors()
        {
            Start();
            Assert.Equal(0, Last(GarageCentralLogicFactory.OutputOpenCount));
            Input(GarageCentralLogicFactory.InputStates[0], (int)GarageState.Open);
            Input(GarageCentralLogicFactory.InputStates[3], (int)GarageState.Closing);
            Input(GarageCentralLogicFactory.InputStates[5], (int)GarageState.Closed);

            Assert.Equal(2, Last(GarageCentralLogicFactory.OutputOpenCount));
            Assert.Equal(1, Last(GarageCentralLogicFactory.OutputClosedCount));
        }

        [Fact]
        public void Lock_StopsAll_AndBlocksCommands()
        {
            Start();
            Input(GarageCentralLogicFactory.InputLock, true);
            Assert.Equal((int)GarageCommand.Stop, Command);
            Input(GarageCentralLogicFactory.InputCompleteOpen, true);
            VisuInput(GarageCentralLogicFactory.InputCompleteOpen, true);
            Assert.Equal((int)GarageCommand.Stop, Command);

            Input(GarageCentralLogicFactory.InputLock, false);
            Input(GarageCentralLogicFactory.InputCompleteOpen, true);
            Assert.Equal((int)GarageCommand.Open, Command);
        }

        [Fact]
        public void DisablePeriphery_OnlyVisuCommands()
        {
            Start();
            Input(GarageCentralLogicFactory.InputDisablePeriphery, true);
            Input(GarageCentralLogicFactory.InputCompleteOpen, true);
            Assert.Empty(Values(GarageCentralLogicFactory.OutputCommand));

            VisuInput(GarageCentralLogicFactory.InputCompleteClose, true);
            Assert.Equal((int)GarageCommand.Close, Command);
        }
    }
}
