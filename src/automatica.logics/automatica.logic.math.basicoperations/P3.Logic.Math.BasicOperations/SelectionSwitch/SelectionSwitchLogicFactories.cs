using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.SelectionSwitch
{
    /// <summary>
    /// The interface ids of one selection switch variant; <see cref="Minus"/> is empty for the (+) variant
    /// </summary>
    internal sealed record SelectionSwitchIds(
        Guid Plus,
        Guid Minus,
        Guid Set,
        Guid Off,
        Guid Disable,
        Guid Output,
        Guid ParamMinimum,
        Guid ParamMaximum,
        Guid ParamStep,
        Guid ParamRepeat,
        Guid ParamDefault);

    internal static class SelectionSwitchTemplates
    {
        internal const double DefaultMinimum = 1;
        internal const double DefaultMaximum = 10;
        internal const double DefaultStep = 1;
        internal const double DefaultRepeat = 0.2;
        internal const double DefaultValue = 1;

        internal static void Create(ILogicTemplateFactory factory, Guid logic, string key, SelectionSwitchIds ids)
        {
            var prefix = $"MATH.{key}";
            factory.CreateLogicInterfaceTemplate(ids.Plus, "+", $"{prefix}.PLUS.DESCRIPTION", logic, LogicInterfaceDirection.Input, 1, 1);
            var sort = 2;
            if (ids.Minus != Guid.Empty)
            {
                factory.CreateLogicInterfaceTemplate(ids.Minus, "-", $"{prefix}.MINUS.DESCRIPTION", logic, LogicInterfaceDirection.Input, 1, sort++);
            }
            factory.CreateLogicInterfaceTemplate(ids.Set, "V", $"{prefix}.SET.DESCRIPTION", logic, LogicInterfaceDirection.Input, 1, sort++);
            factory.CreateLogicInterfaceTemplate(ids.Off, "Off", $"{prefix}.OFF.DESCRIPTION", logic, LogicInterfaceDirection.Input, 1, sort++);
            factory.CreateLogicInterfaceTemplate(ids.Disable, "DisPc", $"{prefix}.DISABLE.DESCRIPTION", logic, LogicInterfaceDirection.Input, 1, sort);

            factory.CreateLogicInterfaceTemplate(ids.Output, "O", $"{prefix}.OUTPUT.DESCRIPTION", logic, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(ids.ParamMinimum, $"{prefix}.MINIMUM.NAME", $"{prefix}.MINIMUM.DESCRIPTION", "vmin", logic, 1, RuleInterfaceParameterDataType.Double, DefaultMinimum);
            factory.CreateParameterLogicInterfaceTemplate(ids.ParamMaximum, $"{prefix}.MAXIMUM.NAME", $"{prefix}.MAXIMUM.DESCRIPTION", "vmax", logic, 2, RuleInterfaceParameterDataType.Double, DefaultMaximum);
            factory.CreateParameterLogicInterfaceTemplate(ids.ParamStep, $"{prefix}.STEP.NAME", $"{prefix}.STEP.DESCRIPTION", "sts", logic, 3, RuleInterfaceParameterDataType.Double, DefaultStep);
            factory.CreateParameterLogicInterfaceTemplate(ids.ParamRepeat, $"{prefix}.REPEAT.NAME", $"{prefix}.REPEAT.DESCRIPTION", "rr", logic, 4, RuleInterfaceParameterDataType.Double, DefaultRepeat);
            factory.CreateParameterLogicInterfaceTemplate(ids.ParamDefault, $"{prefix}.DEFAULT.NAME", $"{prefix}.DEFAULT.DESCRIPTION", "vdef", logic, 5, RuleInterfaceParameterDataType.Double, DefaultValue);
        }
    }

    public class SelectionSwitchPlusLogicFactory : LogicFactory
    {
        public static readonly Guid RulePlus = new Guid("e9517cbf-f9a5-4e1f-b41b-9d667455bdff");
        public static readonly Guid RuleSet = new Guid("0f52a871-aa0e-42bf-b456-9c53e2a592e8");
        public static readonly Guid RuleOff = new Guid("83cf5403-d474-4bce-9f3b-dd2fd7427a97");
        public static readonly Guid RuleDisable = new Guid("b899543c-d997-4a83-97a4-0069b52544b1");
        public static readonly Guid RuleOutput = new Guid("20183cab-9a57-4c40-bcfc-0943e21d9aad");
        public static readonly Guid RuleParamMinimum = new Guid("43bfa513-3671-4dd2-98f0-8bd995f11214");
        public static readonly Guid RuleParamMaximum = new Guid("3c33b5e2-5dde-42b9-80ec-db0112f9f7a9");
        public static readonly Guid RuleParamStep = new Guid("ce9d9d17-6f07-404d-b2ca-e026b5169072");
        public static readonly Guid RuleParamRepeat = new Guid("cb084572-876f-4378-8450-9500ba8cdc7e");
        public static readonly Guid RuleParamDefault = new Guid("59cca9a5-f815-4355-816d-dd2033aba244");

        internal static readonly SelectionSwitchIds Ids = new(RulePlus, Guid.Empty, RuleSet, RuleOff, RuleDisable, RuleOutput,
            RuleParamMinimum, RuleParamMaximum, RuleParamStep, RuleParamRepeat, RuleParamDefault);

        public override string LogicName => "Math.SelectionSwitchPlus";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("6643f4f2-e215-4ada-8d52-eaca102f68f6");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.SELECTION_SWITCH_PLUS.NAME", "MATH.SELECTION_SWITCH_PLUS.DESCRIPTION", "math.selection_switch_plus", "MATH.NAME", 100, 100);
            SelectionSwitchTemplates.Create(factory, LogicGuid, "SELECTION_SWITCH_PLUS", Ids);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SelectionSwitchRule(context, Ids, wrap: true);
        }
    }

    public class SelectionSwitchPlusMinusLogicFactory : LogicFactory
    {
        public static readonly Guid RulePlus = new Guid("396ccfb8-a311-4ec8-9715-0d8b3105acc1");
        public static readonly Guid RuleMinus = new Guid("bf866831-343e-4404-8cb3-8a17a871fa8f");
        public static readonly Guid RuleSet = new Guid("68d08b2a-aea7-4ca3-9bd1-e9d9a33757b2");
        public static readonly Guid RuleOff = new Guid("7efc0a40-9e3c-492a-a4b0-58aca937e6f4");
        public static readonly Guid RuleDisable = new Guid("295d4bc2-c81c-46fe-b740-55653b95d4dd");
        public static readonly Guid RuleOutput = new Guid("671121a6-db92-484b-8437-62f58b63312e");
        public static readonly Guid RuleParamMinimum = new Guid("37564c00-3c18-4616-9544-2b832a357692");
        public static readonly Guid RuleParamMaximum = new Guid("e80601a1-e354-4c7c-82b9-996fd356488e");
        public static readonly Guid RuleParamStep = new Guid("68be9dfa-97ab-4c4e-859d-a9c4b3a72e51");
        public static readonly Guid RuleParamRepeat = new Guid("d7b9283f-300d-4668-8cc5-4ee30a1ed488");
        public static readonly Guid RuleParamDefault = new Guid("ef69b70d-bc71-4cbf-887e-d9d6bd245670");

        internal static readonly SelectionSwitchIds Ids = new(RulePlus, RuleMinus, RuleSet, RuleOff, RuleDisable, RuleOutput,
            RuleParamMinimum, RuleParamMaximum, RuleParamStep, RuleParamRepeat, RuleParamDefault);

        public override string LogicName => "Math.SelectionSwitchPlusMinus";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("f6daf2e7-a0d1-42e7-a3de-8214a5000398");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.SELECTION_SWITCH_PLUS_MINUS.NAME", "MATH.SELECTION_SWITCH_PLUS_MINUS.DESCRIPTION", "math.selection_switch_plus_minus", "MATH.NAME", 100, 100);
            SelectionSwitchTemplates.Create(factory, LogicGuid, "SELECTION_SWITCH_PLUS_MINUS", Ids);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SelectionSwitchRule(context, Ids, wrap: false);
        }
    }
}
