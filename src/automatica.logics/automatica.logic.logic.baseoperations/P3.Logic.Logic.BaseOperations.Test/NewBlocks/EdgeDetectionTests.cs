using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.EdgeDetection;
using P3.Logic.Logic.BaseOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.NewBlocks
{
    public class EdgeDetectionTests : LogicTest<EdgeDetectionLogicFactory>
    {
        private readonly RecordingDispatcher _recorder = new();

        // the logic under test dispatches into its own recording dispatcher, not into the shared one of the base class
        private new ILogic Logic { get; }

        public EdgeDetectionTests()
        {
            Logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, _recorder));
            Logic.Start().GetAwaiter().GetResult();
        }

        private void Send(object value) => Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(EdgeDetectionLogicFactory.RuleInput), Dispatchable, value));

        [Fact]
        public void RisingAndFalling_SendPulses()
        {
            Send(true);
            Assert.Equal(new object[] { true, false }, _recorder.ValuesOf(EdgeDetectionLogicFactory.RuleRising));
            Assert.Empty(_recorder.ValuesOf(EdgeDetectionLogicFactory.RuleFalling));

            Send(false);
            Assert.Equal(new object[] { true, false }, _recorder.ValuesOf(EdgeDetectionLogicFactory.RuleFalling));
        }

        [Fact]
        public void RepeatedValues_SendNothing()
        {
            Send(false);
            Send(0);
            Assert.Empty(_recorder.ValuesOf(EdgeDetectionLogicFactory.RuleRising));
            Assert.Empty(_recorder.ValuesOf(EdgeDetectionLogicFactory.RuleFalling));

            Send(1);
            Send(5);
            Assert.Equal(new object[] { true, false }, _recorder.ValuesOf(EdgeDetectionLogicFactory.RuleRising));
        }
    }
}
