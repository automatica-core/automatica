using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.VkingBms.DriverFactory
{
    public class VkingBmsDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "VkingBms";
        public override Guid DriverGuid => new Guid("ad95d8f8-7393-4631-afba-7e241f1ecdd4");
        public override Version DriverVersion => new Version(0, 2, 0, 4);


        public Guid PackInterface => DriverGuid;
        public Guid BatteryPackTypeGuid => new("d1fc1ea8-da07-4880-a77b-a1348ddef349");
        public Guid BatteryPackCellTypeGuid => new("9426130a-f182-455d-be00-92c6bebc16a2");
        public Guid BatteryPackTemperatureTypeGuid => new("62227e69-70a0-47e8-a950-a950a14796c6");

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(PackInterface, "VKING_BMS.DRIVER.NAME", "VKING_BMS.DRIVER.DESCRIPTION", 16, int.MaxValue,
                true);
            factory.CreateInterfaceType(BatteryPackTypeGuid, "VKING_BMS.PACK.NAME", "VKING_BMS.PACK.DESCRIPTION", 12, 16,
                true);
            factory.CreateInterfaceType(BatteryPackCellTypeGuid, "VKING_BMS.PACK_CELL.NAME", "VKING_BMS.PACK_CELL.DESCRIPTION", 16, 1,
                true);
            factory.CreateInterfaceType(BatteryPackTemperatureTypeGuid, "VKING_BMS.PACK_TEMP.NAME", "VKING_BMS.PACK_TEMP.DESCRIPTION", 6, 1,
                true);

            factory.CreateNodeTemplate(PackInterface, "VKING_BMS.DRIVER.NAME", "VKING_BMS.DRIVER.DESCRIPTION",
                "vking-bms-pack", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs232), PackInterface, false, false, true, false, true,
                NodeDataType.NoAttribute, 16, false);
            factory.CreatePropertyTemplate(new Guid("e9d7d9cb-24bc-405e-8201-c734552a60df"), "COMMON.PROPERTY.INTERFACE.NAME",
                "COMMON.PROPERTY.INTERFACE.DESCRIPTION", "vking-pack-port", PropertyTemplateType.Interface, PackInterface,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreateNodeTemplate(BatteryPackTypeGuid, "VKING_BMS.PACK.NAME", "VKING_BMS.PACK.DESCRIPTION",
                "vking-bms-pack", PackInterface, BatteryPackTypeGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, 16, false);
            factory.CreatePropertyTemplate(new Guid("668d134b-cfea-4152-9242-a9c19c3948b9"), "COMMON.PROPERTY.ID.NAME",
                "COMMON.PROPERTY.ID.DESCRIPTION", "vking-pack-id", PropertyTemplateType.Integer, BatteryPackTypeGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, 1, 1, 1);

            factory.CreateNodeTemplate(BatteryPackCellTypeGuid, "VKING_BMS.CELL.NAME", "VKING_BMS.CELL.DESCRIPTION",
                "vking-bms-pack-cells", BatteryPackTypeGuid, BatteryPackCellTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(BatteryPackTemperatureTypeGuid, "VKING_BMS.TEMPERATURE.NAME", "SOLARMAN.TEMPERATURE.DESCRIPTION",
                "vking-bms-pack-cells-temperatures", BatteryPackTypeGuid, BatteryPackTemperatureTypeGuid, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);


            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("1ec08873-c603-4b29-b1f9-ed2fe56ad586"), "VOLTAGE", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("4dd4161d-2899-4bf9-87ae-0626c02d080a"), "CURRENT", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("f4f20b7a-cfc6-42ae-9559-b7e50233acaa"), "SOC", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("64e273f8-6c0c-4a1a-bd9c-dcfa5e3640b4"), "SOH", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("6f3ccd46-1fba-44ae-9956-83324b1b3a0f"), "REMAIN_CAPACITY", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("82e1c057-6bc4-4183-a10f-255b0af367ea"), "FULL_CHARGE", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("4bb09883-ecf6-4770-a5ee-a6b1e30ae719"), "CYCLE_TIMES", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("418f65ce-74ff-473b-b728-15097d3d2cb4"), "LAST_UPDATE", NodeDataType.DateTime);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("984b0327-8642-45c6-8515-11de469f4707"), "VERSION", NodeDataType.String);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("d13a5599-b48c-46e5-9310-1353343e2c11"), "BMS_TIME", NodeDataType.DateTime);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("9889c744-130a-49e9-b72e-e556184a30e4"), "CELL_MIN", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("cf1c02ae-cbf9-41b9-b97d-91a249e73937"), "CELL_MAX", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTypeGuid, new Guid("70eefbd8-b8c3-4a2b-b73e-94f5c68fd92b"), "CELL_DIFFERENCE", NodeDataType.Double);

            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("1ac72695-4187-404e-b3fa-3b0359c4c165"), "CELL_1", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("cb9a353b-f5ee-4ede-aa29-6977ef29c9cf"), "CELL_2", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("f3615b39-43ba-482d-986c-36dc17b1702b"), "CELL_3", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("bed3a842-fc60-4879-9374-7ae60527a1d2"), "CELL_4", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("a459809e-e943-49e7-9b0b-7d2b10317ae2"), "CELL_5", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("be15ae37-ece8-4913-857e-09d60d22fb07"), "CELL_6", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("ecf9dfce-280e-4d72-9f14-d0520de574ad"), "CELL_7", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("6a83dfe3-4219-4c45-bbdc-977b3ab5f647"), "CELL_8", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("b5c8a165-3c56-4f58-b29e-957e292ce568"), "CELL_9", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("77fddba6-7793-400e-a7af-24ed59ba6ddd"), "CELL_10", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("a3622480-9e72-4907-a033-cfcaae0d1967"), "CELL_11", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("80ee41da-f0bb-4f02-add7-404dd2b2d980"), "CELL_12", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("8a26868f-a470-4de4-a6c2-35d86ce55f09"), "CELL_13", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("f3c24882-cbe9-485c-932b-8bba450919a8"), "CELL_14", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("58c3a50a-1ac4-4ca3-bf5c-11eda5b46343"), "CELL_15", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackCellTypeGuid, new Guid("4e9de2fa-4234-4159-b0be-ef86983d4321"), "CELL_16", NodeDataType.Double);

            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("74ff58e1-0f42-4a73-b176-0a33529adc92"), "TEMP_ENV", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("253bafcb-d9ce-4782-a9f8-d31f40031c35"), "TEMP_MOS", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("d87ef1d7-336e-42b7-ad3c-bd526f8bbfa6"), "TEMP_CELL_T1", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("5823185b-093f-469e-81d4-968c1b1c5342"), "TEMP_CELL_T2", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("18a1b9d5-0fda-4805-818a-a77f1388f7e2"), "TEMP_CELL_T3", NodeDataType.Double);
            CreateValueTemplate(factory, BatteryPackTemperatureTypeGuid, new Guid("07bbb32e-3742-4858-9db5-7854090eb888"), "TEMP_CELL_T4", NodeDataType.Double);

        }

        private void CreateValueTemplate(INodeTemplateFactory factory, Guid needsInterfaceGuid, Guid guid, string name, NodeDataType nodeDataType)
        {
            factory.CreateNodeTemplate(guid, $"VKING_BMS.CELL.{name.ToUpperInvariant()}.NAME", $"SVKING_BMS.CELL.{name.ToUpperInvariant()}.DESCRIPTION", $"vking-bms-pack-cells-{name.ToLowerInvariant()}", needsInterfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, nodeDataType, 1, false, true);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new VkingDriverNode(config);
        }
    }

}
