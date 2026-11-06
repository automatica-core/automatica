using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.HueBridge;
using P3.Driver.HueBridgeSimulatorDriverFactory;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.HueBridgeSimulator.DriverFactory
{
    public class HueBridgeSimulatorDriverFactory : Automatica.Core.Driver.DriverFactory
    {

        public static readonly Guid BusId = new Guid("1998f2fa-a6d9-4c52-873a-57e10d1afcd6");

        public static readonly Guid InterfaceId = new Guid("4fe4efff-277c-4003-ada7-29bb4a09a333");

        public static readonly Guid OnOffLight = new Guid("263feb11-749a-4d55-a340-12723e0ae6b9");
        public static readonly Guid OnOffLightState = new Guid("ddf584d2-0679-4090-8a66-54d264b45fd4");
        public static readonly Guid OnOffLightSwitch = new Guid("87cb16ac-f766-4a73-9b25-2fb3a1f96e22");

        public static readonly Guid OnOffLightOn = new Guid("ddf584d2-0679-4090-8a66-54d264b45fd4");
        public static readonly Guid OnOffLightOff = new Guid("15d8a83f-0e17-4347-b96e-dddd45c422d5");

        public const string BindAddressKey = "hue-bind-address";
        public const string PortKey = "hue-port";
        public const string UsersKey = "hue-users";
        public const string LinkButtonKey = "hue-link-button";
        public const string RemoveUsersKey = "hue-remove-users";

        public override string DriverName => "hue-bridge-simulator";
        public override string ImageName => "automaticacore/plugin-p3.driver.huebridge";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 2, 0, 0);
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "HUE_BRIDGE_SIMULATOR.NAME", "HUE_BRIDGE_SIMULATOR.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateInterfaceType(OnOffLight, "HUE_BRIDGE_SIMULATOR.ON_OFF.NAME", "HUE_BRIDGE_SIMULATOR.ON_OFF.DESCRIPTION", int.MaxValue, 1, true);


            factory.CreateNodeTemplate(BusId, "HUE_BRIDGE_SIMULATOR.NAME", "HUE_BRIDGE_SIMULATOR.DESCRIPTION", "hue-bridge-simulator", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("6f0b8a64-5a0e-4c39-9d0e-8c3c1f0b3e11"), "COMMON.PROPERTY.IP.NAME", "HUE_BRIDGE_SIMULATOR.BIND_ADDRESS.DESCRIPTION", BindAddressKey,
                PropertyTemplateType.Ip, BusId, "COMMON.CATEGORY.ADDRESS", true, false, null, "0.0.0.0", 0, 0);
            factory.CreatePropertyTemplate(new Guid("0b4d9a8e-0e0a-4b4f-a7a8-3e8b5d1f6a22"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION", PortKey,
                PropertyTemplateType.Integer, BusId, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), HueDriver.DefaultPort, 0, 1);
            factory.CreatePropertyTemplate(new Guid("c5a1d0e9-3c62-4f0e-9a2b-7d4e8f1a5b33"), "HUE_BRIDGE_SIMULATOR.USERS.NAME", "HUE_BRIDGE_SIMULATOR.USERS.DESCRIPTION", UsersKey,
                PropertyTemplateType.Password, BusId, "", false, true, null, "", 0, 0);
            factory.CreatePropertyTemplate(new Guid("e2f7c3b1-8d4a-4e6f-b0c9-1a2b3c4d5e44"), "HUE_BRIDGE_SIMULATOR.LINK_BUTTON.NAME", "HUE_BRIDGE_SIMULATOR.LINK_BUTTON.DESCRIPTION", LinkButtonKey,
                PropertyTemplateType.CustomAction, BusId, "HUE_BRIDGE_SIMULATOR.PAIRING", true, true, null, null, 1, 0);
            factory.CreatePropertyTemplate(new Guid("9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c55"), "HUE_BRIDGE_SIMULATOR.REMOVE_USERS.NAME", "HUE_BRIDGE_SIMULATOR.REMOVE_USERS.DESCRIPTION", RemoveUsersKey,
                PropertyTemplateType.CustomAction, BusId, "HUE_BRIDGE_SIMULATOR.PAIRING", true, true, null, null, 1, 1);

            factory.CreateNodeTemplate(OnOffLight, "HUE_BRIDGE_SIMULATOR.ON_OFF.NAME", "HUE_BRIDGE_SIMULATOR.ONF_OFF.DESCRIPTION", "hue-onoff", InterfaceId, OnOffLight, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            factory.CreateNodeTemplate(OnOffLightState, "HUE_BRIDGE_SIMULATOR.ON_OFF.STATE.NAME", "HUE_BRIDGE_SIMULATOR.ON_OFF.STATE.DESCRIPTION", "hue-onoff-state", OnOffLight, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(OnOffLightSwitch, "HUE_BRIDGE_SIMULATOR.ON_OFF.SWITCH.NAME", "HUE_BRIDGE_SIMULATOR.ONF_OFF.SWITCH.DESCRIPTION", "hue-onoff-switch", OnOffLight, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new HueBridgeDriver(config);
        }
    }
}
