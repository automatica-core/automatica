using System;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Rules;

namespace Automatica.Core.UnitTests.Base.Rules
{
    public class RuleTest<T> where T : RuleFactory
    {
        public T Instance { get; }
        protected RuleTemplateFactoryMock Factory { get; }

        protected IRule Rule { get; }
        protected RuleInstance RuleInstance { get;  }
        public RuleContextMock Context { get; }
        protected DispatchableMock Dispatchable { get; }

        public RuleTest()
        {
            Instance = Activator.CreateInstance<T>();

            Factory = new RuleTemplateFactoryMock();

            Dispatchable = new DispatchableMock();

            Instance.InitTemplates(Factory);

            RuleInstance = Factory.CreateRuleInstanceFromTemplate(Instance.RuleGuid);

            Context = new RuleContextMock(RuleInstance, DispatcherMock.Instance);

            Rule = Instance.CreateRuleInstance(Context);
            Rule.Start();
        }

        public void RuleInputChanged(RuleInterfaceInstance instance, object value)
        {
            var valueChanges = Rule.ValueChanged(instance, Dispatchable, value);

            foreach (var valueChange in valueChanges)
            {
                Context.Dispatcher.DispatchValue(valueChange.Instance, valueChange.Value);
            }
        }

        protected RuleInterfaceInstance GetRuleInterfaceByTemplate(Guid templateGuid)
        {
            foreach (var i in RuleInstance.RuleInterfaceInstance)
            {
                if (i.This2RuleInterfaceTemplate == templateGuid)
                {
                    return i;
                }
            }
            return null;
        }
    }
}
