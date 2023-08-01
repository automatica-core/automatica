using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Addition;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Addition
{
    public class AdditionTests: LogicTest<AdditionLogicFactory>
    {
        [Fact]
        public async Task TestAdditionRule()
        {
            await Context.Dispatcher.ClearValues();

            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput1), 1);
            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance).Values;
            Assert.Equal(1.0, values.First().Value);

            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput2), 10);
            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput3),  1);
            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput4),  10);
            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput1),  0);


            LogicInputChanged(GetLogicInterfaceByTemplate(AdditionLogicFactory.RuleInput1), null);
            Assert.Equal(21.0, Context.Dispatcher.GetValues(DispatchableType.RuleInstance).Values.First().Value);
        }
    }
}
