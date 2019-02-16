using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace Automatica.Core.UnitTests.Rules
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

            Context = new RuleContextMock(RuleInstance);

            Rule = Instance.CreateRuleInstance(Context);
            Rule.Start();
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
