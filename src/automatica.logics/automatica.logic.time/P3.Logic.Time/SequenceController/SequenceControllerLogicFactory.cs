using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Time.SequenceController
{
    /// <summary>
    /// Sequence controller (Loxone "Sequence Controller"): sequences of steps written as text, see <see cref="SequenceProgram"/>.
    /// </summary>
    public class SequenceControllerLogicFactory : LogicFactory
    {
        internal const int DefaultIntervalMs = 500;
        internal const int MinIntervalMs = 20;
        internal const int MaxIntervalMs = 1000;

        internal const string DefaultProgram = "sequence 1\n// set AQ1 = AI1 * 2\n";

        /// <summary>
        /// Meta of a text parameter that the property editor shows as a multi-line code editor
        /// </summary>
        internal const string MultilineMeta = "MULTILINE";

        public static readonly Guid[] RuleStartSequence =
        {
            new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a01"), new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a02"),
            new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a03"), new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a04"),
            new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a05"), new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a06"),
            new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a07"), new("1f4a7c2e-6b3d-4e8f-9a1b-2c3d4e5f6a08")
        };

        public static readonly Guid[] RuleInputs =
        {
            new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b01"), new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b02"),
            new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b03"), new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b04"),
            new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b05"), new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b06"),
            new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b07"), new("2e5b8d3f-7c4e-4f90-8b2c-3d4e5f6a7b08")
        };

        public static readonly Guid[] RuleOutputs =
        {
            new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c01"), new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c02"),
            new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c03"), new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c04"),
            new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c05"), new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c06"),
            new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c07"), new("3f6c9e4a-8d5f-4a01-9c3d-4e5f6a7b8c08")
        };

        public static readonly Guid RuleSelect = new("4a7d0f5b-9e6a-4b12-8d4e-5f6a7b8c9d10");
        public static readonly Guid RuleOff = new("4a7d0f5b-9e6a-4b12-8d4e-5f6a7b8c9d11");

        public static readonly Guid RuleActiveSequence = new("5b8e1a6c-0f7b-4c23-9e5f-6a7b8c9d0e20");
        public static readonly Guid RuleLine = new("5b8e1a6c-0f7b-4c23-9e5f-6a7b8c9d0e21");
        public static readonly Guid RuleText = new("5b8e1a6c-0f7b-4c23-9e5f-6a7b8c9d0e22");
        public static readonly Guid RuleErrors = new("5b8e1a6c-0f7b-4c23-9e5f-6a7b8c9d0e23");

        public static readonly Guid RuleParamProgram = new("6c9f2b7d-1a8c-4d34-8f6a-7b8c9d0e1f30");
        public static readonly Guid RuleParamInterval = new("6c9f2b7d-1a8c-4d34-8f6a-7b8c9d0e1f31");

        public override string LogicName => "Time.SequenceController";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("7d0a3c8e-2b9d-4e45-9a7b-8c9d0e1f2a40");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.SEQUENCE_CONTROLLER.NAME", "TIME.SEQUENCE_CONTROLLER.DESCRIPTION", "time.sequence_controller", "TIME.NAME", 340, 160);

            var sort = 1;
            for (var i = 0; i < RuleStartSequence.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleStartSequence[i], $"S{i + 1}", "TIME.SEQUENCE_CONTROLLER.START.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, sort++);
            }
            for (var i = 0; i < RuleInputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleInputs[i], $"AI{i + 1}", "TIME.SEQUENCE_CONTROLLER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, sort++);
            }
            factory.CreateLogicInterfaceTemplate(RuleSelect, "S", "TIME.SEQUENCE_CONTROLLER.SELECT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, sort++);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.SEQUENCE_CONTROLLER.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, sort);

            sort = 1;
            for (var i = 0; i < RuleOutputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleOutputs[i], $"AQ{i + 1}", "TIME.SEQUENCE_CONTROLLER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++);
            }
            factory.CreateLogicInterfaceTemplate(RuleActiveSequence, "S", "TIME.SEQUENCE_CONTROLLER.ACTIVE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleLine, "L", "TIME.SEQUENCE_CONTROLLER.LINE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleText, "TQ", "TIME.SEQUENCE_CONTROLLER.TEXT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleErrors, "Err", "TIME.SEQUENCE_CONTROLLER.ERRORS.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamProgram, "TIME.SEQUENCE_CONTROLLER.PROGRAM.NAME", "TIME.SEQUENCE_CONTROLLER.PROGRAM.DESCRIPTION", "program", LogicGuid, 1,
                RuleInterfaceParameterDataType.Text, DefaultProgram, false, MultilineMeta);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamInterval, "TIME.SEQUENCE_CONTROLLER.INTERVAL.NAME", "TIME.SEQUENCE_CONTROLLER.INTERVAL.DESCRIPTION", "interval", LogicGuid, 2,
                RuleInterfaceParameterDataType.Integer, DefaultIntervalMs, false);
        }

        internal static int IndexOf(Guid[] templates, Guid template) => Array.IndexOf(templates, template);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SequenceControllerRule(context);
        }
    }
}
