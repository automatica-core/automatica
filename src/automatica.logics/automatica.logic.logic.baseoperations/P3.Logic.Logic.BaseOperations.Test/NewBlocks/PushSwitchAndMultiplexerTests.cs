using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.Multiplexer;
using P3.Logic.Logic.BaseOperations.PushSwitch;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.NewBlocks
{
    public class PushSwitchTests : LogicTest<PushSwitchLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void Toggle_OnEveryOne_IgnoresZero()
        {
            Assert.Equal(true, Send(PushSwitchLogicFactory.RuleToggle, true)[0].Value);
            Assert.Empty(Send(PushSwitchLogicFactory.RuleToggle, false));
            Assert.Equal(false, Send(PushSwitchLogicFactory.RuleToggle, true)[0].Value);
            // a button that sends only 1 still toggles every time
            Assert.Equal(true, Send(PushSwitchLogicFactory.RuleToggle, 1)[0].Value);
            Assert.Equal(false, Send(PushSwitchLogicFactory.RuleToggle, 1)[0].Value);
        }

        [Fact]
        public void SetAndReset_AreAbsolute()
        {
            Assert.Equal(true, Send(PushSwitchLogicFactory.RuleSet, true)[0].Value);
            Assert.Equal(true, Send(PushSwitchLogicFactory.RuleSet, true)[0].Value);
            Assert.Equal(false, Send(PushSwitchLogicFactory.RuleReset, true)[0].Value);
            Assert.Equal(false, Send(PushSwitchLogicFactory.RuleReset, true)[0].Value);
            Assert.Equal(true, Send(PushSwitchLogicFactory.RuleToggle, true)[0].Value);
        }
    }

    public class MultiplexerTests : LogicTest<MultiplexerLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void PassesSelectedInput()
        {
            Assert.Empty(Send(MultiplexerLogicFactory.RuleInput1, 10));
            Send(MultiplexerLogicFactory.RuleInput3, "abc");

            Assert.Equal(10, Send(MultiplexerLogicFactory.RuleSelect, 1)[0].Value);
            Assert.Equal("abc", Send(MultiplexerLogicFactory.RuleSelect, 3.0)[0].Value);

            // other inputs do not reach the output
            Assert.Empty(Send(MultiplexerLogicFactory.RuleInput1, 11));
            Assert.Equal("def", Send(MultiplexerLogicFactory.RuleInput3, "def")[0].Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-1)]
        [InlineData("x")]
        public void SelectorOutOfRange_SendsNothing(object selector)
        {
            Send(MultiplexerLogicFactory.RuleInput1, 10);
            Assert.Empty(Send(MultiplexerLogicFactory.RuleSelect, selector));
        }

        [Fact]
        public void SelectedInputWithoutValue_SendsNothing()
        {
            Assert.Empty(Send(MultiplexerLogicFactory.RuleSelect, 2));
        }
    }
}
