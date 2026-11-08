using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.OperatingModes
{
    /// <summary>
    /// Operating mode (Loxone "Operating Modes" as a digital input/output): 1 while a system operating mode is active,
    /// the input switches the mode on manually.
    /// </summary>
    public class OperatingModeLogicFactory : LogicFactory
    {
        public static readonly Guid InputOn = new Guid("249102e9-ffa2-45ac-9337-1a1ef79e614a");
        public static readonly Guid OutputActive = new Guid("7fbf5d72-80df-4327-83c4-5a50882f9c13");
        public static readonly Guid OutputModes = new Guid("f73b8b3d-9e7e-46c7-bf07-e213d3551bde");
        public static readonly Guid ParamMode = new Guid("07ae93c1-bf48-4f4e-8849-d5494fb35cda");

        public override string LogicName => "Time.OperatingMode";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("afb8cc73-fdfc-4cb0-862f-9b17e3d9f2ef");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.OPERATING_MODE.NAME", "TIME.OPERATING_MODE.DESCRIPTION", "time.operating_mode", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputOn, "On", "TIME.OPERATING_MODE.ON.DESCRIPTION", "on", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputActive, "A", "TIME.OPERATING_MODE.ACTIVE.DESCRIPTION", "active", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputModes, "M", "TIME.OPERATING_MODE.MODES.DESCRIPTION", "modes", LogicGuid, LogicInterfaceDirection.Output, 0, 2);

            factory.CreateParameterLogicInterfaceTemplate(ParamMode, "TIME.OPERATING_MODE.MODE.NAME", "TIME.OPERATING_MODE.MODE.DESCRIPTION", "mode", LogicGuid, 1,
                RuleInterfaceParameterDataType.Text, OperatingModeContract.PublicHoliday, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new OperatingModeRule(context);
        }
    }
}
