using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Division;
using P3.Logic.Math.BasicOperations.Modulo;
using P3.Logic.Math.BasicOperations.Multiply;
using P3.Logic.Math.BasicOperations.Subtract;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests
{
    internal static class StartValueTestHelper
    {
        internal static object OutputValue<T>(LogicTest<T> test, Guid outputTemplate) where T : LogicFactory
        {
            var output = test.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == outputTemplate);
            return test.Context.Dispatcher.GetValues(DispatchableType.RuleInstance)
                .Single(a => a.Key == output.ObjId).Value.Value;
        }

        internal static void Input<T>(LogicTest<T> test, ILogic logic, IDispatchable source, Guid inputTemplate, object value) where T : LogicFactory
        {
            logic.ValueChanged(test.Context.RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == inputTemplate), source, value);
        }
    }

    public class DivisionStartTests : LogicTest<DivisionLogicFactory>
    {
        [Fact]
        public async Task TestStartDispatchesQuotientAndKeepsInputsOverRestart()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();
            Assert.Equal(1.0, StartValueTestHelper.OutputValue(this, DivisionLogicFactory.RuleOutput));

            StartValueTestHelper.Input(this, Logic, Dispatchable, DivisionLogicFactory.RuleInput1, 10);
            StartValueTestHelper.Input(this, Logic, Dispatchable, DivisionLogicFactory.RuleInput2, 4);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(2.5, StartValueTestHelper.OutputValue(this, DivisionLogicFactory.RuleOutput));
        }
    }

    public class MultiplyStartTests : LogicTest<MultiplyLogicFactory>
    {
        [Fact]
        public async Task TestStartDispatchesProductAndKeepsInputsOverRestart()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();
            Assert.Equal(1.0, StartValueTestHelper.OutputValue(this, MultiplyLogicFactory.RuleOutput));

            StartValueTestHelper.Input(this, Logic, Dispatchable, MultiplyLogicFactory.RuleInput1, 10);
            StartValueTestHelper.Input(this, Logic, Dispatchable, MultiplyLogicFactory.RuleInput2, 4);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(40.0, StartValueTestHelper.OutputValue(this, MultiplyLogicFactory.RuleOutput));
        }
    }

    public class SubtractStartTests : LogicTest<SubtractLogicFactory>
    {
        [Fact]
        public async Task TestStartDispatchesDifferenceAndKeepsInputsOverRestart()
        {
            StartValueTestHelper.Input(this, Logic, Dispatchable, SubtractLogicFactory.RuleInput1, 10);
            StartValueTestHelper.Input(this, Logic, Dispatchable, SubtractLogicFactory.RuleInput2, 4);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(6.0, StartValueTestHelper.OutputValue(this, SubtractLogicFactory.RuleOutput));
        }
    }

    public class ModuloStartTests : LogicTest<ModuloLogicFactory>
    {
        [Fact]
        public async Task TestStartDispatchesModuloAndKeepsInputsOverRestart()
        {
            StartValueTestHelper.Input(this, Logic, Dispatchable, ModuloLogicFactory.RuleInput1, 10);
            StartValueTestHelper.Input(this, Logic, Dispatchable, ModuloLogicFactory.RuleInput2, 4);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(2.0, StartValueTestHelper.OutputValue(this, ModuloLogicFactory.RuleOutput1));
            Assert.Equal(2, StartValueTestHelper.OutputValue(this, ModuloLogicFactory.RuleOutput2));
        }

        [Fact]
        public void TestDivisorZeroDoesNotThrow()
        {
            StartValueTestHelper.Input(this, Logic, Dispatchable, ModuloLogicFactory.RuleInput1, 10);
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput2), Dispatchable, 0);

            // like Division: the double result is not a number, the integer output has no value
            Assert.Equal(2, result.Count);
            Assert.True(double.IsNaN((double)result[0].Value));
            Assert.Null(result[1].Value);
        }
    }
}
