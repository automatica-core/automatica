using System;
using Automatica.Core.Base.Calendar;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Common;

namespace Automatica.Core.UnitTests.Base.Logics
{
    public class LogicTest<T> where T : LogicFactory
    {
        public T Instance { get; }
        protected LogicTemplateFactoryMock Factory { get; }

        protected ILogic Logic { get; }
        protected RuleInstance RuleInstance { get;  }
        public LogicContextMock Context { get; }
        protected DispatchableMock Dispatchable { get; }

        protected FakeTimeProvider FakeTimeProvider => FakeTimeProvider.Instance;

        public LogicTest()
        {
            DateTimeHelper.ProviderInstance = FakeTimeProvider.Instance;
            
            Instance = Activator.CreateInstance<T>();

            Factory = new LogicTemplateFactoryMock();

            Dispatchable = new DispatchableMock();

            Instance.InitTemplates(Factory);

            RuleInstance = Factory.CreateRuleInstanceFromTemplate(Instance.LogicGuid);

            Context = new LogicContextMock(RuleInstance, Factory, DispatcherMock.Instance);

            Logic = Instance.CreateLogicInstance(Context);
            Logic.Start();
        }

        public void LogicInputChanged(RuleInterfaceInstance instance, object value)
        {
            var valueChanges = Logic.ValueChanged(instance, Dispatchable, value);

            foreach (var valueChange in valueChanges)
            {
                Context.Dispatcher.DispatchValue(valueChange.Instance, valueChange.Value);
            }
        }

        protected RuleInterfaceInstance GetLogicInterfaceByTemplate(Guid templateGuid)
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
