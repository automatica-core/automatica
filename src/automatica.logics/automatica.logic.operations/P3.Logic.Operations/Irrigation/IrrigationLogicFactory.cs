using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Irrigation
{
    /// <summary>
    /// Irrigation: Waters up to 8 zones one after the other with a pump; skips or shortens a run after rain or when rain is forecast (modelled on the Loxone function block of the same name)
    /// </summary>
    public class IrrigationLogicFactory : LogicFactory
    {
        public static readonly Guid InputActivate = new Guid("34338a5b-d943-5827-8b52-6d6a11f9fb19");
        public static readonly Guid InputStart = new Guid("75cabd50-9d81-5a87-9da2-33900d6af2f8");
        public static readonly Guid InputStop = new Guid("2e428f54-07e1-58bf-afb2-a9d43a8d98a8");
        public static readonly Guid InputSelect = new Guid("0a48a035-fb97-5177-a870-ffe012587f61");
        public static readonly Guid InputRainForecast = new Guid("43862002-3f60-5e9d-ba82-6ec0b247e1a4");
        public static readonly Guid InputRain = new Guid("323efa3b-8902-5561-8db3-7a0f9bbd10b3");
        public static readonly Guid InputLock = new Guid("faddf002-4241-5a51-bec1-035229b4824e");
        public static readonly Guid OutputPump = new Guid("e4b71298-045e-5bfe-a1bd-00f0cd1b6d10");
        public static readonly Guid OutputValve1 = new Guid("a5f47de9-8c76-5d52-a6c5-768e730c676b");
        public static readonly Guid OutputValve2 = new Guid("f9bf0723-2bb9-5ddd-a001-d495918d075a");
        public static readonly Guid OutputValve3 = new Guid("783cd014-8e04-5764-b202-4927eaa6ffce");
        public static readonly Guid OutputValve4 = new Guid("21bb7c17-3569-592b-8dfd-0e4647604c5d");
        public static readonly Guid OutputValve5 = new Guid("0b6fdfa7-f9ef-57e3-a21b-a8ea4f87acf2");
        public static readonly Guid OutputValve6 = new Guid("dfff7929-bb9b-5aa9-86ba-81cc3d3a074e");
        public static readonly Guid OutputValve7 = new Guid("2763bbae-bc09-5a75-90bc-4783fa8b408d");
        public static readonly Guid OutputValve8 = new Guid("76a4a26b-5067-50e4-bbcf-56237b259af3");
        public static readonly Guid OutputActiveValve = new Guid("6a9e7ebe-3dc2-5e39-8aa0-ebb78956014d");
        public static readonly Guid OutputRunning = new Guid("a963e858-b686-541e-89dd-c1427026a154");
        public static readonly Guid OutputRemaining = new Guid("14206a34-5a28-5b92-9353-caaf22ce2ec8");
        public static readonly Guid OutputSkipped = new Guid("9e296245-5ea7-55cb-a389-302058d29722");
        public static readonly Guid OutputRainTime = new Guid("a8402f1f-d23d-583d-900a-6c644a255bcd");
        public static readonly Guid ParamMaxRainForecast = new Guid("c9a913f3-5ff2-58b7-b781-ffc403a69510");
        public static readonly Guid ParamMaxRainTime = new Guid("bc74733e-87d3-5821-af97-5faa913eb6c1");
        public static readonly Guid ParamAdjustByRain = new Guid("aa4a8a25-3890-5365-bc02-ef4904dfda9e");
        public static readonly Guid ParamSchedule = new Guid("a3164e3d-30dc-5c80-94e5-7bfd8b888718");
        public static readonly Guid ParamScheduleEnabled = new Guid("50f01ea4-2760-58b3-99de-84a00da33885");
        public static readonly Guid ParamZoneNames = new Guid("f37496e2-2d69-5858-a0e8-f71df0cfd19d");
        public static readonly Guid ParamDuration1 = new Guid("76e2ad06-42fb-5ea5-8568-163602187861");
        public static readonly Guid ParamDuration2 = new Guid("6f0165d2-ed3e-5acd-b0d5-8030dd05b248");
        public static readonly Guid ParamDuration3 = new Guid("da13d10f-cf92-5db8-b9f9-b5ca24b9a794");
        public static readonly Guid ParamDuration4 = new Guid("d2630912-1e4c-57a0-9347-9b0d4ba59e80");
        public static readonly Guid ParamDuration5 = new Guid("bf795fa1-1f86-53c2-b976-0ef3e9a32d91");
        public static readonly Guid ParamDuration6 = new Guid("a0a7e7c0-e0a4-5dc9-9f54-76d0ff579a08");
        public static readonly Guid ParamDuration7 = new Guid("3491969a-9143-51f5-b88d-bec0c504f305");
        public static readonly Guid ParamDuration8 = new Guid("cd09d2b0-e6e0-5ddb-b000-c25e7a5ead95");

        public static readonly Guid[] OutputValves = { OutputValve1, OutputValve2, OutputValve3, OutputValve4, OutputValve5, OutputValve6, OutputValve7, OutputValve8 };
        public static readonly Guid[] ParamDurations = { ParamDuration1, ParamDuration2, ParamDuration3, ParamDuration4, ParamDuration5, ParamDuration6, ParamDuration7, ParamDuration8 };

        public override string LogicName => "P3.Logic.Operations.Irrigation";
        public override Guid LogicGuid => new Guid("274fa92f-3585-5d8b-a3fe-a8575d7df8a0");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.IRRIGATION.NAME", "OPERATIONS.IRRIGATION.DESCRIPTION", "operations-irrigation", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputActivate, "OPERATIONS.IRRIGATION.INPUT.ACTIVATE.NAME", "OPERATIONS.IRRIGATION.INPUT.ACTIVATE.DESCRIPTION", "activate", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputStart, "OPERATIONS.IRRIGATION.INPUT.START.NAME", "OPERATIONS.IRRIGATION.INPUT.START.DESCRIPTION", "start", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputStop, "OPERATIONS.IRRIGATION.INPUT.STOP.NAME", "OPERATIONS.IRRIGATION.INPUT.STOP.DESCRIPTION", "stop", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputSelect, "OPERATIONS.IRRIGATION.INPUT.SELECT.NAME", "OPERATIONS.IRRIGATION.INPUT.SELECT.DESCRIPTION", "select", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputRainForecast, "OPERATIONS.IRRIGATION.INPUT.RAIN_FORECAST.NAME", "OPERATIONS.IRRIGATION.INPUT.RAIN_FORECAST.DESCRIPTION", "rainForecast", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputRain, "OPERATIONS.IRRIGATION.INPUT.RAIN.NAME", "OPERATIONS.IRRIGATION.INPUT.RAIN.DESCRIPTION", "rain", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputLock, "OPERATIONS.IRRIGATION.INPUT.LOCK.NAME", "OPERATIONS.IRRIGATION.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(OutputPump, "OPERATIONS.IRRIGATION.OUTPUT.PUMP.NAME", "OPERATIONS.IRRIGATION.OUTPUT.PUMP.DESCRIPTION", "pump", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputValve1, "OPERATIONS.IRRIGATION.OUTPUT.VALVE1.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE1.DESCRIPTION", "valve1", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputValve2, "OPERATIONS.IRRIGATION.OUTPUT.VALVE2.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE2.DESCRIPTION", "valve2", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputValve3, "OPERATIONS.IRRIGATION.OUTPUT.VALVE3.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE3.DESCRIPTION", "valve3", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateLogicInterfaceTemplate(OutputValve4, "OPERATIONS.IRRIGATION.OUTPUT.VALVE4.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE4.DESCRIPTION", "valve4", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputValve5, "OPERATIONS.IRRIGATION.OUTPUT.VALVE5.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE5.DESCRIPTION", "valve5", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputValve6, "OPERATIONS.IRRIGATION.OUTPUT.VALVE6.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE6.DESCRIPTION", "valve6", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateLogicInterfaceTemplate(OutputValve7, "OPERATIONS.IRRIGATION.OUTPUT.VALVE7.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE7.DESCRIPTION", "valve7", LogicGuid, LogicInterfaceDirection.Output, 0, 8);
            factory.CreateLogicInterfaceTemplate(OutputValve8, "OPERATIONS.IRRIGATION.OUTPUT.VALVE8.NAME", "OPERATIONS.IRRIGATION.OUTPUT.VALVE8.DESCRIPTION", "valve8", LogicGuid, LogicInterfaceDirection.Output, 0, 9);
            factory.CreateLogicInterfaceTemplate(OutputActiveValve, "OPERATIONS.IRRIGATION.OUTPUT.ACTIVE_VALVE.NAME", "OPERATIONS.IRRIGATION.OUTPUT.ACTIVE_VALVE.DESCRIPTION", "activeValve", LogicGuid, LogicInterfaceDirection.Output, 0, 10);
            factory.CreateLogicInterfaceTemplate(OutputRunning, "OPERATIONS.IRRIGATION.OUTPUT.RUNNING.NAME", "OPERATIONS.IRRIGATION.OUTPUT.RUNNING.DESCRIPTION", "running", LogicGuid, LogicInterfaceDirection.Output, 0, 11);
            factory.CreateLogicInterfaceTemplate(OutputRemaining, "OPERATIONS.IRRIGATION.OUTPUT.REMAINING.NAME", "OPERATIONS.IRRIGATION.OUTPUT.REMAINING.DESCRIPTION", "remaining", LogicGuid, LogicInterfaceDirection.Output, 0, 12);
            factory.CreateLogicInterfaceTemplate(OutputSkipped, "OPERATIONS.IRRIGATION.OUTPUT.SKIPPED.NAME", "OPERATIONS.IRRIGATION.OUTPUT.SKIPPED.DESCRIPTION", "skipped", LogicGuid, LogicInterfaceDirection.Output, 0, 13);
            factory.CreateLogicInterfaceTemplate(OutputRainTime, "OPERATIONS.IRRIGATION.OUTPUT.RAIN_TIME.NAME", "OPERATIONS.IRRIGATION.OUTPUT.RAIN_TIME.DESCRIPTION", "rainTime", LogicGuid, LogicInterfaceDirection.Output, 0, 14);
            factory.CreateParameterLogicInterfaceTemplate(ParamMaxRainForecast, "OPERATIONS.IRRIGATION.PARAM.MAX_RAIN_FORECAST.NAME", "OPERATIONS.IRRIGATION.PARAM.MAX_RAIN_FORECAST.DESCRIPTION", "maxRainForecast", LogicGuid, 1, RuleInterfaceParameterDataType.Double, 2.0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamMaxRainTime, "OPERATIONS.IRRIGATION.PARAM.MAX_RAIN_TIME.NAME", "OPERATIONS.IRRIGATION.PARAM.MAX_RAIN_TIME.DESCRIPTION", "maxRainTime", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, 1800, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamAdjustByRain, "OPERATIONS.IRRIGATION.PARAM.ADJUST_BY_RAIN.NAME", "OPERATIONS.IRRIGATION.PARAM.ADJUST_BY_RAIN.DESCRIPTION", "adjustByRain", LogicGuid, 3, RuleInterfaceParameterDataType.Bool, false, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamSchedule, "OPERATIONS.IRRIGATION.PARAM.SCHEDULE.NAME", "OPERATIONS.IRRIGATION.PARAM.SCHEDULE.DESCRIPTION", "schedule", LogicGuid, 4, RuleInterfaceParameterDataType.Timer, Newtonsoft.Json.JsonConvert.SerializeObject(new TimerPropertyData()), false);
            factory.CreateParameterLogicInterfaceTemplate(ParamScheduleEnabled, "OPERATIONS.IRRIGATION.PARAM.SCHEDULE_ENABLED.NAME", "OPERATIONS.IRRIGATION.PARAM.SCHEDULE_ENABLED.DESCRIPTION", "scheduleEnabled", LogicGuid, 5, RuleInterfaceParameterDataType.Bool, false, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamZoneNames, "OPERATIONS.IRRIGATION.PARAM.ZONE_NAMES.NAME", "OPERATIONS.IRRIGATION.PARAM.ZONE_NAMES.DESCRIPTION", "zoneNames", LogicGuid, 6, RuleInterfaceParameterDataType.Text, "Zone 1;Zone 2;Zone 3;Zone 4;Zone 5;Zone 6;Zone 7;Zone 8", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration1, "OPERATIONS.IRRIGATION.PARAM.DURATION1.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION1.DESCRIPTION", "duration1", LogicGuid, 7, RuleInterfaceParameterDataType.Integer, 600, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration2, "OPERATIONS.IRRIGATION.PARAM.DURATION2.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION2.DESCRIPTION", "duration2", LogicGuid, 8, RuleInterfaceParameterDataType.Integer, 600, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration3, "OPERATIONS.IRRIGATION.PARAM.DURATION3.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION3.DESCRIPTION", "duration3", LogicGuid, 9, RuleInterfaceParameterDataType.Integer, 600, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration4, "OPERATIONS.IRRIGATION.PARAM.DURATION4.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION4.DESCRIPTION", "duration4", LogicGuid, 10, RuleInterfaceParameterDataType.Integer, 600, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration5, "OPERATIONS.IRRIGATION.PARAM.DURATION5.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION5.DESCRIPTION", "duration5", LogicGuid, 11, RuleInterfaceParameterDataType.Integer, 0, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration6, "OPERATIONS.IRRIGATION.PARAM.DURATION6.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION6.DESCRIPTION", "duration6", LogicGuid, 12, RuleInterfaceParameterDataType.Integer, 0, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration7, "OPERATIONS.IRRIGATION.PARAM.DURATION7.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION7.DESCRIPTION", "duration7", LogicGuid, 13, RuleInterfaceParameterDataType.Integer, 0, true);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration8, "OPERATIONS.IRRIGATION.PARAM.DURATION8.NAME", "OPERATIONS.IRRIGATION.PARAM.DURATION8.DESCRIPTION", "duration8", LogicGuid, 14, RuleInterfaceParameterDataType.Integer, 0, true);

            // the tile of this block is a core visu template (enum value 22); older cores without it keep the default logic tile,
            // so the plugin still builds and runs against the published core NuGet
            if (Enum.GetName(typeof(VisuMobileObjectTemplateTypes), 22) == "Irrigation")
            {
                factory.ChangeDefaultVisuTemplate(LogicGuid, (VisuMobileObjectTemplateTypes)22);
            }
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new IrrigationLogic(context);
        }
    }
}
