using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Automatica.Remote
{
    public class AutomaticaRemoteDriverFactory : DriverFactory
    {
        public override string DriverName => "Automatica.Remote.Driver";
        public override Version DriverVersion => new Version(0, 1, 0, 9);

        private static readonly Guid IpProperty = new Guid("91119590-cae6-489b-9764-9166820d31f8");
        private static readonly Guid ScanProperty = new Guid("1c1d45f4-8fe0-4a3d-b703-238522ea03d9");
        private static readonly Guid AutomaticaDeviceInterface = new Guid("d0f44f99-ab19-4480-a90f-adeac889f56b");
        public static readonly Guid AutomaticaBoardInterface = new Guid("a78ad1af-fc5e-4327-b06c-3eb483db374e");
        public static readonly Guid AutomaticaBoardInterfaceInterface = new Guid("f68e73f4-9fdb-4d87-8754-2dd52ef0d12a");

        public override Guid DriverGuid => new Guid("282efc61-c451-4671-a349-788910bdeb66");
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Board), "Board", "", int.MaxValue, int.MaxValue, false);

            

            factory.CreateInterfaceType(AutomaticaDeviceInterface, "Automatica.Remote.Device", "", int.MaxValue, 1, true);
            factory.CreateInterfaceType(AutomaticaBoardInterface, "Automatica.Remote.Device.Board", "", 1, 1, true);
            factory.CreateInterfaceType(AutomaticaBoardInterfaceInterface, "Automatica.Remote.Device.Board.Interface", "", 1, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "Automatica.Device", "", "automatica-device",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), AutomaticaDeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute,
                Int32.MaxValue, false);


            factory.CreatePropertyTemplate(IpProperty, "AUTOMATICA_REMOTE.IP.NAME", "AUTOMATICA_REMOTE.IP.DESCRIPTION",
                "automatica.remote.ip", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("4db70f0f-12ef-4cde-8844-d86b66d94a84"), "AUTOMATICA_REMOTE.PORT.NAME", "AUTOMATICA_REMOTE.PORT.DESCRIPTION",
                "automatica.remote.port", PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, ushort.MaxValue), "5001", 1, 1);

            factory.CreatePropertyTemplate(new Guid("4742b192-fd71-47ab-a640-7db60054baf1"), "AUTOMATICA_REMOTE.USER.NAME", "AUTOMATICA_REMOTE.USER.DESCRIPTION",
                "automatica.remote.user", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1, 1);

            factory.CreatePropertyTemplate(new Guid("0de1444e-02de-4e5a-b171-03a287135702"), "AUTOMATICA_REMOTE.PASSWORD.NAME", "AUTOMATICA_REMOTE.PASSWORD.DESCRIPTION",
                "automatica.remote.password", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1, 1);


            factory.CreatePropertyTemplate(ScanProperty, "Scan", "Scan the device for datapoints",
                "automatica.remote.scan", PropertyTemplateType.Scan, DriverGuid, "Recovery", true, false, "", "", 1, 1);

            factory.CreateNodeTemplate(new Guid("121d76c8-e046-4d0b-b9c2-bc45d1b0c744"), "AUTOMATICA.REMOTE.BOARD_TYPE.NAME",
                "AUTOMATICA.REMOTE.BOARD_TYPE.DESCRIPTION", "automatica-device-board", AutomaticaDeviceInterface,
                AutomaticaBoardInterface, false, false, true, false, true, NodeDataType.NoAttribute, 0, false);

            factory.CreateNodeTemplate(new Guid("9db04aa0-8962-4764-aae1-1348801e75c5"), "AUTOMATICA.REMOTE.BOARD_INTERFACE.NAME",
                "AUTOMATICA.REMOTE.BOARD_INTERFACE.DESCRIPTION", "automatica-device-board", AutomaticaBoardInterface,
                AutomaticaBoardInterfaceInterface, false, false, true, false, true, NodeDataType.NoAttribute, 0, false);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
             return new AutomaticaRemoteDriver(config);
        }
    }
}
