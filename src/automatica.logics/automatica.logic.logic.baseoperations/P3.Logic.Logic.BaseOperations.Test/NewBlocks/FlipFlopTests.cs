using System;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.FlipFlop;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.NewBlocks
{
    public class RsFlipFlopTests : LogicTest<RsFlipFlopLogicFactory>
    {
        private object Send(Guid template, object value)
        {
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);
            Assert.Single(result);
            Assert.Equal(RsFlipFlopLogicFactory.RuleOutput, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            return result[0].Value;
        }

        [Fact]
        public void Set_Holds_UntilReset()
        {
            Assert.Equal(true, Send(RsFlipFlopLogicFactory.RuleSet, true));
            Assert.Equal(true, Send(RsFlipFlopLogicFactory.RuleSet, false));
            Assert.Equal(false, Send(RsFlipFlopLogicFactory.RuleReset, 1));
            Assert.Equal(false, Send(RsFlipFlopLogicFactory.RuleReset, 0));
        }

        [Fact]
        public void BothSet_ResetWins()
        {
            Send(RsFlipFlopLogicFactory.RuleSet, true);
            Assert.Equal(false, Send(RsFlipFlopLogicFactory.RuleReset, true));
            // releasing R while S is still 1 sets again
            Assert.Equal(true, Send(RsFlipFlopLogicFactory.RuleReset, false));
        }

        [Fact]
        public void StringAndNumberValues_AreBooleans()
        {
            Assert.Equal(true, Send(RsFlipFlopLogicFactory.RuleSet, "true"));
            Assert.Equal(true, Send(RsFlipFlopLogicFactory.RuleSet, "0"));
            Assert.Equal(false, Send(RsFlipFlopLogicFactory.RuleReset, 2.5));
        }
    }

    public class SrFlipFlopTests : LogicTest<SrFlipFlopLogicFactory>
    {
        private object Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value)[0].Value;

        [Fact]
        public void BothSet_SetWins()
        {
            Send(SrFlipFlopLogicFactory.RuleReset, true);
            Assert.Equal(true, Send(SrFlipFlopLogicFactory.RuleSet, true));
            Assert.Equal(false, Send(SrFlipFlopLogicFactory.RuleSet, false));
        }

        [Fact]
        public void Factory_HasDistinctIds()
        {
            Assert.NotEqual(new RsFlipFlopLogicFactory().LogicGuid, new SrFlipFlopLogicFactory().LogicGuid);
            Assert.NotEqual(RsFlipFlopLogicFactory.RuleSet, SrFlipFlopLogicFactory.RuleSet);
        }
    }
}
