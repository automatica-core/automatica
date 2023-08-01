using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using System;

namespace P3.Logic.Surveillance.WindowMonitor
{
    public class WindowMonitorFactory : LogicFactory
    {
        public override string LogicName => "WindowMonitor";

        public override Guid LogicGuid => new Guid("b664d32f-3b21-40b4-80a8-608491e07a5f");

        public override Version LogicVersion => new Version(0, 0, 0, 4);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new WindowMonitorLogic(context);
        }

        public static readonly Guid Ct = new Guid("56e789c7-4a25-4a71-ac75-7bf2fcc647e3");
        public static readonly Guid Co = new Guid("bef5e164-47db-47d5-9aa1-3ecd578640e9");
        public static readonly Guid Cl = new Guid("1545c638-08e8-4c53-9847-5f2e597dbd1f");
        public static readonly Guid Cti = new Guid("eef56230-7980-4dde-9ac1-246b11469021");
        public static readonly Guid Coi = new Guid("db541c1d-4b57-4002-9408-c687aaba60f9");
        public static readonly Guid Cli = new Guid("b12368f9-8a24-419f-9454-6cb26697085f");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "SURVEILLANCE.WINDOW_MONITOR.NAME", "MATH.WINDOW_MONITOR.DESCRIPTION",
               "surveillance.window_monitor", "SUVREILLANCE.NAME", 100, 100);


            factory.CreateLogicInterfaceTemplate(Ct, "Ct", "SURVEILLANCE.WINDOW_MONITOR.TILT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 2);
            factory.CreateLogicInterfaceTemplate(Co, "Co", "SURVEILLANCE.WINDOW_MONITOR.OPEN.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 3);
            factory.CreateLogicInterfaceTemplate(Cl, "Cl", "SURVEILLANCE.WINDOW_MONITOR.LOCKED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 4);


            factory.CreateLogicInterfaceTemplate(Cti, "Cti", "SURVEILLANCE.WINDOW_MONITOR.TILT_INVERTED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 5);
            factory.CreateLogicInterfaceTemplate(Coi, "Coi", "SURVEILLANCE.WINDOW_MONITOR.OPEN_INVERTED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 6);
            factory.CreateLogicInterfaceTemplate(Cli, "Cli", "SURVEILLANCE.WINDOW_MONITOR.LOCKED_INVERTED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 7);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.WindowMonitor);
        }
    }
}
