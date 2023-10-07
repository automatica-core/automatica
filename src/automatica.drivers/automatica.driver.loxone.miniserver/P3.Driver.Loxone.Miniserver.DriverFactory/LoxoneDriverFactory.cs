using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public static Guid PushButtonNode = new Guid("fca388d6-ce88-4d24-b085-d501d742db81");
        public static Guid DimmerNode = new Guid("75c049c1-08e5-4b63-ae21-f5f1f1bcd7b5");
        public static Guid InfoOnlyAnlogNode = new Guid("300587c6-94e7-4dd9-b9d5-b94f18bc3b1c");
        public static Guid SwitchNodeGuid = new Guid("875098f2-470b-432e-b873-c8a66f6f9ed3");
        public static Guid InfoOnlyDigitalNode = new Guid("bbc82d1f-9cef-465d-b0df-7f512952da82");

        public static Guid FolderNodeInterface = new Guid("7defd06f-6d07-41dd-a654-ed6862f0435a");
        public static Guid FolderNode = new Guid("fbfbd4de-cb85-4fd8-b90f-11ba2711182a");

        public override string DriverName => "loxone-miniserver";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 4, 0, 3);

        public static Guid BusId => new Guid("ad2f16c2-3a4a-4de4-8768-caa766b02810");

        public override string ImageName => "automaticacore/plugin-p3.driver.loxone";
        public override bool InDevelopmentMode => false;

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "LOXONE.MINISERVER.NAME", "LOXONE.MINISERVER.DESCRIPTION", int.MaxValue, int.MaxValue, true);

            factory.CreateNodeTemplate(DriverGuid, "LOXONE.MINISERVER.NAME", "LOXONE.MINISERVER.DESCRIPTION", "loxone-miniserver", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("15d8489a-904a-4190-be6b-c7f26e0689f1"), "LOXONE.MINISERVER.PROPERTY.IP.NAME", "LOXONE.MINISERVER.PROPERTY.IP.DESCRIPTION", "ip-address", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("846128c7-1094-4277-8303-e1b16e5deea2"), "LOXONE.MINISERVER.PROPERTY.PORT.NAME", "LOXONE.MINISERVER.PROPERTY.PORT.DESCRIPTION", "port", PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, short.MaxValue), "80", 1, 1);
            factory.CreatePropertyTemplate(new Guid("251af9bc-a8ab-460f-a7c0-c43493e102c4"), "LOXONE.MINISERVER.PROPERTY.USER.NAME", "LOXONE.MINISERVER.PROPERTY.USER.DESCRIPTION", "user", PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("91c6148a-ce94-4240-abf3-2eec52c46e3e"), "LOXONE.MINISERVER.PROPERTY.PASSWORD.NAME", "LOXONE.MINISERVER.PROPERTY.PASSWORD.DESCRIPTION", "password", PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);

            factory.CreatePropertyTemplate(new Guid("f51d380a-2a89-4a70-b63e-35e91ef810b5"), "LOXONE.MINISERVER.PROPERTY.SCAN.NAME", "LOXONE.MINISERVER.PROPERTY.SCAN.DESCRIPTION", "scan", PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("31f637b5-35cb-4208-86a6-9d3c472f16a0"), "COMMON.PROPERTY.ENABLE_TUNNEL.NAME", "COMMON.PROPERTY.ENABLE_TUNNEL.DESCRIPTION",
                "loxone-use-tunnel", PropertyTemplateType.Bool, DriverGuid, "SERVER.REMOTE", true, false, "", false, 1,
                1);
            CreateFolder(factory);

            CreateConnectionState(factory);
            CreateInfoOnlyAnalog(factory);
            CreateSwitch(factory);
            CreateDimmer(factory);
            CreateInfoOnlyDigital(factory);
            CreatePushButton(factory);
        }

        private void CreateFolder(INodeTemplateFactory factory)
        {
            var folderInterface = FolderNodeInterface;
            factory.CreateInterfaceType(folderInterface, "LOXONE.MINISERVER.FOLDER.NAME", "LOXONE.MINISERVER.FOLDER.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            var folderNode = FolderNode;
            factory.CreateNodeTemplate(folderNode, "LOXONE.MINISERVER.FOLDER.NAME", "LOXONE.MINISERVER.FOLDER.DESCRIPTION", "loxone-folder", DriverGuid, FolderNodeInterface, false, true, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false, false);
        }

        private void CreateConnectionState(INodeTemplateFactory factory)
        {
            var connected = new Guid("d101bcce-be51-4aad-bf1f-11d756674b53");
            factory.CreateInterfaceType(connected, "LOXONE.MINISERVER.CONNECTED.NAME", "LOXONE.MINISERVER.CONNECTED.DESCRIPTION", 1, int.MaxValue, false);
            var connectedNode = new Guid("3887cc34-6103-4dc7-aaec-ba30e6e42710");
            factory.CreateNodeTemplate(connectedNode, "LOXONE.MINISERVER.CONNECTED.NAME", "LOXONE.MINISERVER.CONNECTED.DESCRIPTION", "loxone-connected", DriverGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false, false);
        }

        private void CreatePushButton(INodeTemplateFactory factory)
        {
            var pushButton = new Guid("ceae14b6-3574-4df9-9d92-f557912922dc");
            factory.CreateInterfaceType(pushButton, "LOXONE.MINISERVER.PUSH_BUTTON.NAME", "LOXONE.MINISERVER.PUSH_BUTTON.DESCRIPTION", 1, int.MaxValue, false);
            factory.CreateNodeTemplate(PushButtonNode, "LOXONE.MINISERVER.PUSH_BUTTON.NAME", "LOXONE.MINISERVER.PUSH_BUTTON.DESCRIPTION", "loxone-push-button", FolderNodeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, true, false, NodeDataType.Integer, int.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(PushButtonNode, VisuMobileObjectTemplateTypes.ToggleButton);

            CreateUuidProperty(factory, PushButtonNode);
        }

        private void CreateDimmer(INodeTemplateFactory factory)
        {
            var dimmer = new Guid("fd70966f-d53b-4d7d-a625-1b0113364eef");
            factory.CreateInterfaceType(dimmer, "LOXONE.MINISERVER.DIMMER.NAME", "LOXONE.MINISERVER.DIMMER.DESCRIPTION", 1, int.MaxValue, false);
            factory.CreateNodeTemplate(DimmerNode, "LOXONE.MINISERVER.DIMMER.NAME", "LOXONE.MINISERVER.DIMMER.DESCRIPTION", "loxone-dimmer", FolderNodeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, true, false, NodeDataType.Integer, int.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(DimmerNode, VisuMobileObjectTemplateTypes.Slider);

            CreateUuidProperty(factory, DimmerNode);
        }

        private void CreateInfoOnlyAnalog(INodeTemplateFactory factory)
        {
            var infoOnlyAnalog = new Guid("f72d524f-1fdc-4e0b-b1b6-ede9c500e61d");
            factory.CreateInterfaceType(infoOnlyAnalog, "LOXONE.MINISERVER.INFO_ONLY_ANALOG.NAME", "LOXONE.MINISERVER.INFO_ONLY_ANALOG.DESCRIPTION", 1, int.MaxValue, false);
            factory.CreateNodeTemplate(InfoOnlyAnlogNode, "LOXONE.MINISERVER.INFO_ONLY_ANALOG.NAME", "LOXONE.MINISERVER.INFO_ONLY_ANALOG.DESCRIPTION", "loxone-info-only-analog", FolderNodeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, int.MaxValue, false);
            CreateUuidProperty(factory, InfoOnlyAnlogNode);
        }

        private void CreateSwitch(INodeTemplateFactory factory)
        {
            var switchGuid = new Guid("7a99a720-e031-4a76-b7ef-d734cf7e46a3");
            factory.CreateInterfaceType(switchGuid, "LOXONE.MINISERVER.SWITCH.NAME", "LOXONE.MINISERVER.SWITCH.DESCRIPTION", 1, int.MaxValue, false);
            factory.CreateNodeTemplate(SwitchNodeGuid, "LOXONE.MINISERVER.SWITCH.NAME", "LOXONE.MINISERVER.SWITCH.DESCRIPTION", "loxone-switch", FolderNodeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, true, false, NodeDataType.Boolean, int.MaxValue, false);
            factory.ChangeDefaultVisuTemplate(SwitchNodeGuid, VisuMobileObjectTemplateTypes.ToggleButton);

            CreateUuidProperty(factory, SwitchNodeGuid);

        }
        private void CreateInfoOnlyDigital(INodeTemplateFactory factory)
        {
            var infoOnlyDigital = new Guid("3358d7e0-592c-4489-be23-b7f5942c9d2b");
            factory.CreateInterfaceType(infoOnlyDigital, "LOXONE.MINISERVER.INFO_ONLY_DIGITAL.NAME", "LOXONE.MINISERVER.INFO_ONLY_DIGITAL.DESCRIPTION", 1, int.MaxValue, false);
            factory.CreateNodeTemplate(InfoOnlyDigitalNode, "LOXONE.MINISERVER.INFO_ONLY_DIGITAL.NAME", "LOXONE.MINISERVER.INFO_ONLY_DIGITAL.DESCRIPTION", "loxone-info-only-digital", FolderNodeInterface, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, int.MaxValue, false);
            CreateUuidProperty(factory, InfoOnlyDigitalNode);
        }

        private void CreateUuidProperty(INodeTemplateFactory factory, Guid objectRef)
        {
            var gu = GenerateNewGuid(objectRef, 1);
            factory.CreatePropertyTemplate(gu, "LOXONE.MINISERVER.PROPERTY.UUID.NAME", "LOXONE.MINISERVER.PROPERTY.UUID.DESCRIPTION", "uuid", PropertyTemplateType.Text, objectRef, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(GenerateNewGuid(objectRef, 2), "LOXONE.MINISERVER.PROPERTY.STATE.NAME", "LOXONE.MINISERVER.PROPERTY.STATE.DESCRIPTION", "state", PropertyTemplateType.Text, objectRef, "COMMON.CATEGORY.ADDRESS", false, true, null, "", 1, 1);
        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[gu.Length - 1] = (byte)(Convert.ToInt32(gu[gu.Length - 1]) + c);

            return new Guid(gu);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new LoxoneDriver(config); ;
        }

        public override void AfterSave(NodeInstance instance)
        {
            // do nothing
        }
    }
}
