using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.Selection;
using P3.Logic.Logic.BaseOperations.ShiftRegister;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.NewBlocks
{
    internal static class SelectionOutputs
    {
        public static object Of(this IList<ILogicOutputChanged> changes, Guid template) =>
            changes.Single(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == template).Value;

        /// <summary>
        /// "On" outputs of the change set as 1-based numbers
        /// </summary>
        public static int[] OnOutputs(this IList<ILogicOutputChanged> changes, Guid[] outputs) =>
            changes.Where(a => Equals(a.Value, true)).Select(a => Array.IndexOf(outputs, a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate) + 1).ToArray();
    }

    public class SequencerTests : LogicTest<SequencerLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object Sel(IList<ILogicOutputChanged> changes) => changes.Of(SequencerLogicFactory.RuleSelected);

        [Fact]
        public void Trigger_StepsThroughTheOutputs_AndWraps()
        {
            Send(SequencerLogicFactory.RuleParamMax, 3);

            var first = Send(SequencerLogicFactory.RuleTrigger, true);
            Assert.Equal(1, Sel(first));
            Assert.Equal(new[] { 1 }, first.OnOutputs(SequencerLogicFactory.RuleOutputs));

            var second = Send(SequencerLogicFactory.RuleTrigger, true);
            Assert.Equal(2, Sel(second));
            Assert.Equal(false, second.Of(SequencerLogicFactory.RuleOutputs[0]));
            Assert.Equal(true, second.Of(SequencerLogicFactory.RuleOutputs[1]));

            Assert.Empty(Send(SequencerLogicFactory.RuleTrigger, false));
            Assert.Equal(3, Sel(Send(SequencerLogicFactory.RuleTrigger, 1)));
            Assert.Equal(1, Sel(Send(SequencerLogicFactory.RuleTrigger, 1)));
        }

        [Fact]
        public void Position_SelectsDirectly()
        {
            var changes = Send(SequencerLogicFactory.RulePosition, 5);
            Assert.Equal(5, Sel(changes));
            Assert.Equal(0, Sel(Send(SequencerLogicFactory.RulePosition, "0")));
            Assert.Empty(Send(SequencerLogicFactory.RulePosition, 9));
            Assert.Empty(Send(SequencerLogicFactory.RulePosition, "x"));
        }

        [Fact]
        public void Reset_SelectsTheDefault()
        {
            Send(SequencerLogicFactory.RuleParamDefault, 2);
            Send(SequencerLogicFactory.RulePosition, 6);
            Assert.Equal(2, Sel(Send(SequencerLogicFactory.RuleReset, true)));

            Send(SequencerLogicFactory.RuleParamDefault, 0);
            var off = Send(SequencerLogicFactory.RuleReset, true);
            Assert.Equal(0, Sel(off));
            Assert.Empty(off.OnOutputs(SequencerLogicFactory.RuleOutputs));
        }

        [Fact]
        public void Disable_LocksTriggerAndPosition_NotReset()
        {
            Send(SequencerLogicFactory.RuleDisable, true);
            Assert.Empty(Send(SequencerLogicFactory.RuleTrigger, true));
            Assert.Empty(Send(SequencerLogicFactory.RulePosition, 3));
            Assert.Equal(1, Sel(Send(SequencerLogicFactory.RuleReset, true)));

            Send(SequencerLogicFactory.RuleDisable, false);
            Assert.Equal(2, Sel(Send(SequencerLogicFactory.RuleTrigger, true)));
        }
    }

    public class RadioButtonsTests : LogicTest<RadioButtonsLogicFactory>
    {
        private static readonly Guid[] Outputs = RadioButtonsLogicFactory.RuleOutputs;

        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object N(IList<ILogicOutputChanged> changes) => changes.Of(RadioButtonsLogicFactory.RuleNumber);

        [Fact]
        public void Input_ActivatesItsOutput_AndOnlyThatOne()
        {
            var changes = Send(RadioButtonsLogicFactory.RuleInputs[2], true);
            Assert.Equal(3, N(changes));
            Assert.Equal(new[] { 3 }, changes.OnOutputs(Outputs));

            changes = Send(RadioButtonsLogicFactory.RuleInputs[15], true);
            Assert.Equal(16, N(changes));
            Assert.Equal(false, changes.Of(Outputs[2]));
            Assert.Equal(true, changes.Of(Outputs[15]));

            // the active one again changes nothing, a 0 neither
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleInputs[15], true));
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleInputs[0], false));
        }

        [Fact]
        public void NextAndPrevious_WrapOverAllOff()
        {
            Send(RadioButtonsLogicFactory.RuleParamMax, 3);
            Assert.Equal(1, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(2, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(3, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(0, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(3, N(Send(RadioButtonsLogicFactory.RulePrevious, true)));
        }

        [Fact]
        public void SkipZero_JumpsOverAllOff()
        {
            Send(RadioButtonsLogicFactory.RuleParamMax, 2);
            Send(RadioButtonsLogicFactory.RuleParamSkipZero, true);
            Assert.Equal(1, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(2, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(1, N(Send(RadioButtonsLogicFactory.RuleNext, true)));
            Assert.Equal(2, N(Send(RadioButtonsLogicFactory.RulePrevious, true)));
        }

        [Fact]
        public void Max_LimitsTheInputs_ButNotTheVisu()
        {
            Send(RadioButtonsLogicFactory.RuleParamMax, 4);
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleInputs[5], true));
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleSelect, 5));

            Assert.Equal(6, N(Send(RadioButtonsLogicFactory.RuleVisu, 6)));
            // from beyond Max, - goes back to Max
            Assert.Equal(4, N(Send(RadioButtonsLogicFactory.RulePrevious, true)));
        }

        [Fact]
        public void Disable_LeavesTheVisuWorking()
        {
            Send(RadioButtonsLogicFactory.RuleDisable, true);
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleInputs[0], true));
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleNext, true));
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleSelect, 2));
            Assert.Equal(2, N(Send(RadioButtonsLogicFactory.RuleVisu, 2.2)));
        }

        [Fact]
        public void Off_SwitchesAllOff_AndLocks()
        {
            Send(RadioButtonsLogicFactory.RuleInputs[1], true);
            var off = Send(RadioButtonsLogicFactory.RuleOff, true);
            Assert.Equal(0, N(off));
            Assert.Equal(false, off.Of(Outputs[1]));

            Assert.Empty(Send(RadioButtonsLogicFactory.RuleInputs[3], true));
            Assert.Empty(Send(RadioButtonsLogicFactory.RuleVisu, 3));

            Assert.Empty(Send(RadioButtonsLogicFactory.RuleOff, false));
            Assert.Equal(4, N(Send(RadioButtonsLogicFactory.RuleInputs[3], true)));
        }

        [Fact]
        public void VisuData_ContainsLabelsAndRange()
        {
            Send(RadioButtonsLogicFactory.RuleParamLabels, " Off ;Radio;TV;;");
            Send(RadioButtonsLogicFactory.RuleInputs[1], true);

            var data = Assert.IsType<RadioButtonsData>(Logic.GetDataForVisu());
            Assert.Equal(new[] { "Off", "Radio", "TV" }, data.Labels);
            Assert.Equal(0, data.MinValue);
            Assert.Equal(3, data.MaxValue);
            Assert.Equal(2, data.LastValue);

            Send(RadioButtonsLogicFactory.RuleParamLabels, "");
            Send(RadioButtonsLogicFactory.RuleParamMax, 5);
            data = Assert.IsType<RadioButtonsData>(Logic.GetDataForVisu());
            Assert.Empty(data.Labels);
            Assert.Equal(5, data.MaxValue);
        }

        [Fact]
        public void Template_HasSixteenOutputs_AndAVisuInput()
        {
            Assert.Equal(16, Outputs.Distinct().Count());
            Assert.Equal(16, RadioButtonsLogicFactory.RuleInputs.Distinct().Count());
            Assert.NotNull(GetLogicInterfaceByTemplate(RadioButtonsLogicFactory.RuleVisu));
            Assert.All(Outputs, id => Assert.NotNull(GetLogicInterfaceByTemplate(id)));
        }
    }

    public class ShiftRegisterTests : LogicTest<ShiftRegisterLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object Pulse() => Send(ShiftRegisterLogicFactory.RuleTrigger, true).Of(ShiftRegisterLogicFactory.RuleOutput);

        [Fact]
        public void Data_ReachesTheOutputAfterRbPulses()
        {
            Send(ShiftRegisterLogicFactory.RuleParamBit, 3);
            Send(ShiftRegisterLogicFactory.RuleData, true);
            Assert.Equal(false, Pulse());
            Send(ShiftRegisterLogicFactory.RuleData, false);
            Assert.Equal(false, Pulse());
            Assert.Equal(true, Pulse());
            Assert.Equal(false, Pulse());
        }

        [Fact]
        public void ShiftDown_EntersAtTheTop()
        {
            Send(ShiftRegisterLogicFactory.RuleParamBit, 63);
            Send(ShiftRegisterLogicFactory.RuleDirection, true);
            Send(ShiftRegisterLogicFactory.RuleData, 1);
            Assert.Equal(false, Pulse());
            Send(ShiftRegisterLogicFactory.RuleData, 0);
            Assert.Equal(true, Pulse());

            // back up again
            Send(ShiftRegisterLogicFactory.RuleDirection, false);
            Assert.Equal(false, Pulse());
        }

        [Fact]
        public void ZeroOnTrigger_DoesNotShift()
        {
            Send(ShiftRegisterLogicFactory.RuleParamBit, 1);
            Send(ShiftRegisterLogicFactory.RuleData, true);
            Assert.Empty(Send(ShiftRegisterLogicFactory.RuleTrigger, false));
            Assert.Equal(true, Pulse());
        }
    }
}
