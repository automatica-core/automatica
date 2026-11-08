using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.LightingController;

namespace P3.Logic.Lightning.HotelLightingController
{
    /// <summary>
    /// Hotel lighting controller (Loxone "Hotel Lighting Controller"): a lighting controller plus key card switch, room state
    /// (not booked / booked / checked in), door contact and service (cleaning) mode with the room status output.
    /// </summary>
    public class HotelLightingControllerLogicFactory : LogicFactory
    {
        internal const string Prefix = "LIGHTNING.HOTEL_LIGHTING_CONTROLLER";
        internal const double DefaultLeaveTimeout = 60;
        internal const double DefaultServiceDuration = 3600;

        public static readonly LightingControllerIds Ids = LightingControllerIds.Derive("P3.Logic.Lightning.HotelLightingController");

        public static readonly Guid CardSwitch = new("b16e5173-9fc2-4b80-936a-72deafb16001");
        public static readonly Guid RoomState = new("b16e5173-9fc2-4b80-936a-72deafb16002");
        public static readonly Guid Door = new("b16e5173-9fc2-4b80-936a-72deafb16003");
        public static readonly Guid ServiceButton = new("b16e5173-9fc2-4b80-936a-72deafb16004");

        public static readonly Guid Presence = new("b16e5173-9fc2-4b80-936a-72deafb16011");
        public static readonly Guid Service = new("b16e5173-9fc2-4b80-936a-72deafb16012");
        public static readonly Guid ServiceDone = new("b16e5173-9fc2-4b80-936a-72deafb16013");
        public static readonly Guid RoomStatus = new("b16e5173-9fc2-4b80-936a-72deafb16014");

        public static readonly Guid LeaveTimeout = new("b16e5173-9fc2-4b80-936a-72deafb16021");
        public static readonly Guid ServiceDuration = new("b16e5173-9fc2-4b80-936a-72deafb16022");

        public override string LogicName => "HotelLightingController";
        public override Guid LogicGuid => new("b16e5173-9fc2-4b80-936a-72deafb16000");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{Prefix}.NAME", $"{Prefix}.DESCRIPTION", "lightning.hotel_lighting_controller", "LIGHTNING.NAME", 250, 100);
            LightingControllerLogicFactory.CreateControllerInterfaces(factory, LogicGuid, Ids);

            var sort = 50;
            void HotelInput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.INPUT.{name}.NAME", $"{Prefix}.INPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Input, 0, ++sort);
            HotelInput(CardSwitch, "CARD_SWITCH", "cardSwitch");
            HotelInput(RoomState, "ROOM_STATE", "roomState");
            HotelInput(Door, "DOOR", "door");
            HotelInput(ServiceButton, "SERVICE_BUTTON", "serviceButton");

            sort = 50;
            void HotelOutput(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, $"{Prefix}.OUTPUT.{name}.NAME", $"{Prefix}.OUTPUT.{name}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Output, 0, ++sort, RuleInterfaceType.Output);
            HotelOutput(Presence, "PRESENCE", "presence");
            HotelOutput(Service, "SERVICE", "service");
            HotelOutput(ServiceDone, "SERVICE_DONE", "serviceDone");
            HotelOutput(RoomStatus, "ROOM_STATUS", "roomStatus");

            factory.CreateParameterLogicInterfaceTemplate(LeaveTimeout, $"{Prefix}.PARAM.LEAVE_TIMEOUT.NAME", $"{Prefix}.PARAM.LEAVE_TIMEOUT.DESCRIPTION", "leaveTimeout", LogicGuid, 50, RuleInterfaceParameterDataType.Double, DefaultLeaveTimeout, false);
            factory.CreateParameterLogicInterfaceTemplate(ServiceDuration, $"{Prefix}.PARAM.SERVICE_DURATION.NAME", $"{Prefix}.PARAM.SERVICE_DURATION.DESCRIPTION", "serviceDuration", LogicGuid, 51, RuleInterfaceParameterDataType.Double, DefaultServiceDuration, false);

            LightingControllerLogicFactory.UseVisu(factory, LogicGuid);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new HotelLightingControllerLogic(context);
        }
    }
}
