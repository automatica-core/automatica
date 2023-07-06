//using System.Linq;
//using System.Threading;
//using Automatica.Core.UnitTests.Base.Rules;
//using P3.Logic.DigitalToAnalog.StateToImpuls;
//using Xunit;

//namespace P3.Logic.DigitalToAnalog.Tests.StateToImpuls
//{
//    public class StateToImpulsTests: LogicTest<StateToImpulsLogicFactory>
//    {
//        [Fact]
//        public void TestRule()
//        {
//            var value = Logic.ValueChanged(GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.RuleInput), Dispatchable, 1);
//            Thread.Sleep(1200);

//            Assert.Equal(1, value.Count);
//            Assert.Equal(0, value.First().Value);


//        }
//    }
//}
