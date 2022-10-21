using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.WakeOnLan
{
    public class WakeOnLanDriverFactory : DriverFactory
    {

        public static readonly Guid InterfaceId = new Guid("b3f5e879-4fdf-4617-8822-c0de6202b0ab");

        public static readonly Guid BusId = new Guid("f8d52d67-5937-4aba-bf3b-980aeae554e7");
        public static readonly Guid ValueId = new Guid("937af0a9-5c3a-466b-8950-1a694ed58762");
        public static readonly Guid MacId = new Guid("bb2d594b-d619-45f8-a1f9-74ee2677fdbe");

        public override string DriverName => "wake-on-lan";
        public override Guid DriverGuid => BusId;
        public override Version DriverVersion => new Version(0, 1, 0, 2);
        public override string ImageName => "automaticacore/plugin-p3.driver.wake-on-lan";

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(InterfaceId, "WAKE_ON_LAN.NAME", "WAKE_ON_LAN.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(BusId, "WAKE_ON_LAN.NAME", "WAKE_ON_LAN.DESCRIPTION", "wol", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(ValueId, "WAKE_ON_LAN.NODE.NAME", "WAKE_ON_LAN.NODE.DESCRIPTION", "wol", InterfaceId,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, false, true, false, NodeDataType.Boolean, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(MacId, "WAKE_ON_LAN.MAC.NAME", "WAKE_ON_LAN.MAC.DESCRIPTION", "mac",
                PropertyTemplateType.Text, ValueId, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateMaxLengthMetaString(12), null, 0, 5);


        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new WakeOnLanDriver(config);
        }
    }
}
