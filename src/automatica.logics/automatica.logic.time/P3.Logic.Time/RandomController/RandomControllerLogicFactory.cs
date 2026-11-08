using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.RandomController
{
    /// <summary>
    /// Random controller (Loxone "Random Controller"): random on/off with varying intervals, e.g. presence simulation
    /// </summary>
    public class RandomControllerLogicFactory : LogicFactory
    {
        internal const double DefaultMaxOnDelay = 100;
        internal const double DefaultMaxOffDelay = 10;

        public static readonly Guid InputEnable = new Guid("23aff127-ce97-4885-86c1-fd0ed370ab2a");
        public static readonly Guid Output = new Guid("0e9a04ca-5308-4a3c-adca-d6808b43f02e");
        public static readonly Guid ParamMaxOnDelay = new Guid("622ff263-4260-4e09-b8bd-533b0d3d7ae7");
        public static readonly Guid ParamMaxOffDelay = new Guid("000a0c27-aa8b-4df1-817f-674c250dc33d");

        public override string LogicName => "Time.RandomController";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("b8cad682-4f27-4e6e-8aeb-66cbef3a3018");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.RANDOM_CONTROLLER.NAME", "TIME.RANDOM_CONTROLLER.DESCRIPTION", "time.random_controller", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputEnable, "En", "TIME.RANDOM_CONTROLLER.ENABLE.DESCRIPTION", "enable", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(Output, "Ran", "TIME.RANDOM_CONTROLLER.OUTPUT.DESCRIPTION", "output", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(ParamMaxOnDelay, "TIME.RANDOM_CONTROLLER.MAX_ON_DELAY.NAME", "TIME.RANDOM_CONTROLLER.MAX_ON_DELAY.DESCRIPTION", "max_on_delay", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultMaxOnDelay, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamMaxOffDelay, "TIME.RANDOM_CONTROLLER.MAX_OFF_DELAY.NAME", "TIME.RANDOM_CONTROLLER.MAX_OFF_DELAY.DESCRIPTION", "max_off_delay", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultMaxOffDelay, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RandomControllerRule(context);
        }
    }
}
