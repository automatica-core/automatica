using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace Automatica.Core.Tests.Rule
{
    public class RuleTest
    {
        [Fact]
        public void TestRuleInterfaceInstanceValues()
        {
            var ruleTemplateFactoryMock = new RuleTemplateFactoryMock();

            var ruleTemplate = Guid.NewGuid();
            ruleTemplateFactoryMock.CreateRuleTemplate(ruleTemplate, "test", "test", "test", "test", 10, 10);
            var int1 = Guid.NewGuid();
            ruleTemplateFactoryMock.CreateRuleInterfaceTemplate(int1, "int1", "int1", ruleTemplate,
                RuleInterfaceDirection.Input, 0, 1);

            var param1 = Guid.NewGuid();
            ruleTemplateFactoryMock.CreateParameterRuleInterfaceTemplate(param1, "timer", "timer", ruleTemplate, 0,
                RuleInterfaceParameterDataType.Timer, null);

            var rule = ruleTemplateFactoryMock.CreateRuleInstanceFromTemplate(ruleTemplate);
            var timerInterface = rule.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == param1);

            timerInterface.Value = "{\"StartTime\":\"2000 -02-01T16:00:00.322Z\",\"StopTime\":\"2000-02-01T22:00:00.322Z\",\"EnabledDays\":[1,2,3,4,5,6,0]}";


            var timerValue = timerInterface.Value;

            Assert.IsType<TimerPropertyData>(timerValue);


            timerInterface.Value = new TimerPropertyData();

            timerValue = timerInterface.Value;

            Assert.IsType<TimerPropertyData>(timerValue);


        }
    }
}
