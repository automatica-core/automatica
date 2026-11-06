using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Matter.Driver
{
    public class MatterDriverFactory : DriverFactory
    {
        public const string PortKey = "port";
        public const string ControlsKey = "controls";
        public const string StateKey = "matter-state";
        public const string OpenWindowKey = "open-commissioning-window";
        public const string ResetPairingsKey = "reset-pairings";

        public const string PairingCodeNodeKey = "matter-pairing-code";
        public const string QrCodeNodeKey = "matter-qr-code";
        public const string CommissionedNodeKey = "matter-commissioned";
        public const string FabricsNodeKey = "matter-fabrics";

        public override string DriverName => "MatterBridge";

        public override string ImageName => "automaticacore/plugin-p3.driver.matter";

        public override Guid DriverGuid => new("ce14753c-2010-4314-bb46-1b647879eca1");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new MatterDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "MATTER_BRIDGE.NAME", "MATTER_BRIDGE.DESCRIPTION", int.MaxValue, int.MaxValue, true);
            factory.CreateNodeTemplate(DriverGuid, "MATTER_BRIDGE.NAME", "MATTER_BRIDGE.DESCRIPTION", "matter-bridge",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DriverGuid, false, true, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            CreateValueNode(factory, new Guid("614d4451-937f-45bc-9f1e-1a014132dfef"), "MATTER_BRIDGE.PAIRING_CODE", PairingCodeNodeKey, NodeDataType.String);
            CreateValueNode(factory, new Guid("4dac6cc4-e4df-47a8-a249-b43828b3381d"), "MATTER_BRIDGE.QR_CODE", QrCodeNodeKey, NodeDataType.String);
            CreateValueNode(factory, new Guid("c153c661-1a87-4788-abdd-d893f8137366"), "MATTER_BRIDGE.COMMISSIONED", CommissionedNodeKey, NodeDataType.Boolean);
            CreateValueNode(factory, new Guid("8b6abcc3-99b6-4a10-8ff2-9e34e2024158"), "MATTER_BRIDGE.FABRICS", FabricsNodeKey, NodeDataType.Integer);

            factory.CreatePropertyTemplate(new Guid("7fd869bb-3189-47c7-a761-6855315928a6"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION", PortKey,
                PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 5540, 0, 0);

            factory.CreatePropertyTemplate(new Guid("f885f271-941c-44cc-8db5-1cfc4b69f004"), "COMMON.PROPERTY.CONTROLS.NAME", "COMMON.PROPERTY.CONTROLS.DESCRIPTION", ControlsKey,
                PropertyTemplateType.Controls, DriverGuid, "COMMON.CATEGORY.CONTROLS", true, false, null, null, 0, 1);

            factory.CreatePropertyTemplate(new Guid("ffe11e10-68a2-49fe-b566-4d16c763324f"), "MATTER_BRIDGE.STATE.NAME", "MATTER_BRIDGE.STATE.DESCRIPTION", StateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreatePropertyTemplate(new Guid("00324c43-417b-4abe-98ad-0e4466f020a5"), "MATTER_BRIDGE.OPEN_COMMISSIONING_WINDOW.NAME",
                "MATTER_BRIDGE.OPEN_COMMISSIONING_WINDOW.DESCRIPTION", OpenWindowKey, PropertyTemplateType.CustomAction,
                DriverGuid, "MATTER_BRIDGE.PAIRING", true, true, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("36558a39-6aae-4ea0-9523-21a7ce8c2861"), "MATTER_BRIDGE.RESET_PAIRINGS.NAME",
                "MATTER_BRIDGE.RESET_PAIRINGS.DESCRIPTION", ResetPairingsKey, PropertyTemplateType.CustomAction,
                DriverGuid, "MATTER_BRIDGE.PAIRING", true, true, null, null, 1, 2);
        }

        private void CreateValueNode(INodeTemplateFactory factory, Guid id, string key, string nodeKey, NodeDataType type)
        {
            factory.CreateNodeTemplate(id, $"{key}.NAME", $"{key}.DESCRIPTION", nodeKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, type, 1, false, false);
        }
    }
}
