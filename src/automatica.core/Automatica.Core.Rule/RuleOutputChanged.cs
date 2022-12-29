using System;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Implementation for <see cref="IRuleInterfaceInstanceDispatchable"/>
    /// </summary>
    public class RuleInterfaceInstanceDispatchable : IRuleInterfaceInstanceDispatchable
    {
        private readonly RuleInterfaceInstance _instance;

        public RuleInterfaceInstanceDispatchable(RuleInterfaceInstance instance)
        {
            _instance = instance;
        }

        public DispatchableType Type => DispatchableType.RuleInstance;
        public string Name => _instance.This2RuleInstanceNavigation.Name;
        public Guid Id => _instance.ObjId;
        public RuleInterfaceInstance RuleInterfaceInstance => _instance;

        public DispatchableSource Source => DispatchableSource.RuleInstance;
    }

    /// <summary>
    /// Implementation of <see cref="IRuleOutputChanged"/>
    /// </summary>
    public class RuleOutputChanged : IRuleOutputChanged
    {
        public IRuleInterfaceInstanceDispatchable Instance { get; }
        public object Value { get; }
        public string SourceInformation { get; }
        public double ValueDouble { get; }
        public bool ValueBoolean { get; }
        public int ValueInteger { get; }

        public RuleOutputChanged(RuleInterfaceInstance instance, object value, string sourceInformation="")
        {
            Instance = new RuleInterfaceInstanceDispatchable(instance);
            Value = value;
            SourceInformation = sourceInformation;

            if (value != null && double.TryParse(value.ToString(), out double dblValue))
            {
                ValueDouble = dblValue;
            }
            if (value != null && int.TryParse(value.ToString(), out int intValue))
            {
                ValueInteger = intValue;
            }
            if (value != null && Boolean.TryParse(value.ToString(), out bool bValue))
            {
                ValueBoolean = bValue;
            }
        }
    }
}
