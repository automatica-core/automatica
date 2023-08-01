using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.BinaryDecoder;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.BinaryDecoder
{
    public class BinaryDecoderTests: LogicTest<BinaryDecoderLogicFactory>
    {
        [Fact]
        public void TestRule1()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, 1);

            Assert.True(outputs[0].ValueBoolean);
            Assert.False(outputs[1].ValueBoolean);
            Assert.False(outputs[2].ValueBoolean);
            Assert.False(outputs[3].ValueBoolean);
            Assert.False(outputs[4].ValueBoolean);
            Assert.False(outputs[5].ValueBoolean);
            Assert.False(outputs[6].ValueBoolean);
            Assert.False(outputs[7].ValueBoolean);
        }


        [Fact]
        public void TestRule2()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, short.MaxValue);

            Assert.True(outputs[0].ValueBoolean);
            Assert.True(outputs[1].ValueBoolean);
            Assert.True(outputs[2].ValueBoolean);
            Assert.True(outputs[3].ValueBoolean);
            Assert.True(outputs[4].ValueBoolean);
            Assert.True(outputs[5].ValueBoolean);
            Assert.True(outputs[6].ValueBoolean);
            Assert.True(outputs[7].ValueBoolean);
        }

        [Fact]
        public void TestRule3()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, char.MaxValue);

            Assert.True(outputs[0].ValueBoolean);
            Assert.True(outputs[1].ValueBoolean);
            Assert.True(outputs[2].ValueBoolean);
            Assert.True(outputs[3].ValueBoolean);
            Assert.True(outputs[4].ValueBoolean);
            Assert.True(outputs[5].ValueBoolean);
            Assert.True(outputs[6].ValueBoolean);
            Assert.True(outputs[7].ValueBoolean);
        }
        [Fact]
        public void TestRule4()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, byte.MaxValue);

            Assert.True(outputs[0].ValueBoolean);
            Assert.True(outputs[1].ValueBoolean);
            Assert.True(outputs[2].ValueBoolean);
            Assert.True(outputs[3].ValueBoolean);
            Assert.True(outputs[4].ValueBoolean);
            Assert.True(outputs[5].ValueBoolean);
            Assert.True(outputs[6].ValueBoolean);
            Assert.True(outputs[7].ValueBoolean);
        }
        [Fact]
        public void TestRule5()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, sbyte.MaxValue);

            Assert.True(outputs[0].ValueBoolean);
            Assert.True(outputs[1].ValueBoolean);
            Assert.True(outputs[2].ValueBoolean);
            Assert.True(outputs[3].ValueBoolean);
            Assert.True(outputs[4].ValueBoolean);
            Assert.True(outputs[5].ValueBoolean);
            Assert.True(outputs[6].ValueBoolean);
            Assert.False(outputs[7].ValueBoolean);
        }
        [Fact]
        public void TestRule6()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, 0xAA);

            Assert.False(outputs[0].ValueBoolean);
            Assert.True(outputs[1].ValueBoolean);
            Assert.False(outputs[2].ValueBoolean);
            Assert.True(outputs[3].ValueBoolean);
            Assert.False(outputs[4].ValueBoolean);
            Assert.True(outputs[5].ValueBoolean);
            Assert.False(outputs[6].ValueBoolean);
            Assert.True(outputs[7].ValueBoolean);
        }
    }
}
