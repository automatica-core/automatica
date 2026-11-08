using Xunit;
using F = P3.Logic.Operations.WindowCentral.WindowCentralLogicFactory;

namespace P3.Logic.Operations.Tests.GarageMedia
{
    public class WindowCentralTests : BlockTest<F>
    {
        [Fact]
        public void Factory_HasEightFeedbackInputs()
        {
            Assert.Equal(8, F.InputWindowPositions.Length);
            Assert.Equal(9 + 8 + 7 + 1, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void CompleteOpenAndClose_SendCommandAndPosition()
        {
            Start();
            Input(F.InputCompleteOpen, true);
            Input(F.InputCompleteClose, true);
            Input(F.InputCompleteClose, true);

            Assert.Equal(new object[] { true }, Values(F.OutputOpen));
            Assert.Equal(new object[] { true, true }, Values(F.OutputClose));
            Assert.Equal(new object[] { 100.0, 0.0, 0.0 }, Values(F.OutputPosition));
        }

        [Fact]
        public void Position_And_SlightlyOpen_SendThePosition()
        {
            Param(F.ParamSlightlyOpen, 15);
            Start();
            Input(F.InputPosition, 42);
            Input(F.InputPosition, 140);
            Input(F.InputSlightlyOpen, true);

            Assert.Equal(new object[] { 42.0, 100.0, 15.0 }, Values(F.OutputPosition));
        }

        [Fact]
        public void PushAndHold_StopsOnRelease()
        {
            Start();
            Input(F.InputPartialOpen, true);
            Input(F.InputPartialOpen, false);
            Input(F.InputPartialClose, true);
            Input(F.InputPartialClose, false);

            Assert.Equal(new object[] { true }, Values(F.OutputOpen));
            Assert.Equal(new object[] { true }, Values(F.OutputClose));
            Assert.Equal(new object[] { true, true }, Values(F.OutputStop));
            Assert.Empty(Values(F.OutputPosition));
        }

        [Fact]
        public void Toggle_CyclesOpenStopCloseStop()
        {
            Start();
            Input(F.InputToggle, true);
            Input(F.InputToggle, true);
            Input(F.InputToggle, true);
            Input(F.InputToggle, true);
            Input(F.InputToggle, true);

            Assert.Equal(new object[] { true, true }, Values(F.OutputOpen));
            Assert.Equal(new object[] { true }, Values(F.OutputClose));
            Assert.Equal(new object[] { true, true }, Values(F.OutputStop));
        }

        [Fact]
        public void Toggle_ClosesAfterTheWindowsArrivedOpen()
        {
            Start();
            Input(F.InputWindowPositions[0], 0);
            Input(F.InputToggle, true);
            Input(F.InputWindowPositions[0], 100);

            Input(F.InputToggle, true);
            Assert.Empty(Values(F.OutputStop));
            Assert.Equal(new object[] { true }, Values(F.OutputClose));
        }

        [Fact]
        public void WeatherProtection_ClosesAndLocks()
        {
            Start();
            Input(F.InputWeatherProtection, true);
            Assert.Equal(true, Last(F.OutputClose));
            Assert.Equal(0.0, Last(F.OutputPosition));
            Assert.Equal(true, Last(F.OutputLocked));

            Input(F.InputCompleteOpen, true);
            Input(F.InputPosition, 50);
            Assert.Empty(Values(F.OutputOpen));

            Input(F.InputWeatherProtection, false);
            Assert.Equal(false, Last(F.OutputLocked));
            Input(F.InputCompleteOpen, true);
            Assert.Equal(new object[] { true }, Values(F.OutputOpen));
        }

        [Fact]
        public void Lock_StopsAndLocksUntilBothAreOff()
        {
            Start();
            Input(F.InputLock, true);
            Assert.Equal(true, Last(F.OutputStop));
            Input(F.InputWeatherProtection, true);
            Input(F.InputLock, false);
            Assert.Equal(true, Last(F.OutputLocked));
            Input(F.InputWeatherProtection, false);
            Assert.Equal(false, Last(F.OutputLocked));
        }

        [Fact]
        public void Counts_OpenAndClosedWindows()
        {
            Start();
            Input(F.InputWindowPositions[0], 0);
            Input(F.InputWindowPositions[1], 30);
            Input(F.InputWindowPositions[7], 100);

            Assert.Equal(2, Last(F.OutputOpenCount));
            Assert.Equal(1, Last(F.OutputClosedCount));

            Input(F.InputWindowPositions[1], 0);
            Assert.Equal(1, Last(F.OutputOpenCount));
            Assert.Equal(2, Last(F.OutputClosedCount));
        }
    }
}
