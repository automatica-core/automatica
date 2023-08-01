using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.HueBridgeSimulatorDriverFactory;

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
        

        public override string DriverName => "hue-bridge-simulator";
        public override string ImageName => "automaticacore/plugin-p3.driver.huebridge";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 1, 0, 4);
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "HUE_BRIDGE_SIMULATOR.NAME", "HUE_BRIDGE_SIMULATOR.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateInterfaceType(OnOffLight, "HUE_BRIDGE_SIMULATOR.ON_OFF.NAME", "HUE_BRIDGE_SIMULATOR.ON_OFF.DESCRIPTION", int.MaxValue, 1, true);


            factory.CreateNodeTemplate(BusId, "HUE_BRIDGE_SIMULATOR.NAME", "HUE_BRIDGE_SIMULATOR.DESCRIPTION", "hue-bridge-simulator", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

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
