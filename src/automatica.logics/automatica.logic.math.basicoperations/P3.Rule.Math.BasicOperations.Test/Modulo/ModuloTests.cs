using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Modulo;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Modulo
{
    
    public class ModuloTests: LogicTest<ModuloLogicFactory>
    {
        [Fact]
        public void TestModuloRule()
        {
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput1), Dispatchable, 2)[0].Value == null);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput1), Dispatchable, 2)[1].Value == null);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput2), Dispatchable, 5)[0].ValueDouble == 2.0);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput2), Dispatchable, 5)[1].ValueInteger == 2);

            Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput1), Dispatchable, 2.3);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput2), Dispatchable, 5.3)[0].ValueDouble == 2.3);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput2), Dispatchable, 5.3)[1].ValueInteger == 2);

            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput1);
            Assert.True(Logic.ValueChanged(GetLogicInterfaceByTemplate(ModuloLogicFactory.RuleInput1), Dispatchable, 1)[1].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput2);
        }
    }
}
