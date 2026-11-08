using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Analog
{
    public class AnalogMemoryLogicFactory : LogicFactory
    {
        public static readonly Guid RuleValue = new Guid("0bf06149-fc93-4c45-8b92-5cf0cf867c32");
        public static readonly Guid RuleSet = new Guid("c472de07-6c6d-4195-ab04-a5250cf9b082");
        public static readonly Guid RuleOff = new Guid("a97d73af-8a04-474c-a900-32d2207db75c");
        public static readonly Guid RuleOutput = new Guid("1c24ca62-7cf0-4400-8a61-f077a92b72c2");

        public override string LogicName => "Math.AnalogMemory";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("82956c96-5105-4878-843d-d5ca2c881daf");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.ANALOG_MEMORY.NAME", "MATH.ANALOG_MEMORY.DESCRIPTION", "math.analog_memory", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "MATH.ANALOG_MEMORY.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleSet, "Set", "MATH.ANALOG_MEMORY.SET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "MATH.ANALOG_MEMORY.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "V", "MATH.ANALOG_MEMORY.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AnalogMemoryRule(context);
        }
    }

    /// <summary>
    /// Analog memory (Loxone "Analogue Memory"): every 1 on Set copies the current V to the output, which then holds it.
    /// Every 1 on Off sets the output to 0.
    /// </summary>
    public class AnalogMemoryRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private double? _value;

        public AnalogMemoryRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == AnalogMemoryLogicFactory.RuleOutput);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == AnalogMemoryLogicFactory.RuleValue)
            {
                _value = MathValue.ToDouble(value) ?? _value;
            }
            else if (template == AnalogMemoryLogicFactory.RuleSet && MathValue.ToBool(value) && _value.HasValue)
            {
                return SingleOutputChanged(new LogicOutputChanged(_output, _value.Value));
            }
            else if (template == AnalogMemoryLogicFactory.RuleOff && MathValue.ToBool(value))
            {
                return SingleOutputChanged(new LogicOutputChanged(_output, 0d));
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
