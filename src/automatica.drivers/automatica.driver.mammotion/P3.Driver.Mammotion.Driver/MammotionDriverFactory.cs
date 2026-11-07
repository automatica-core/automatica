using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Mammotion.Driver
{
    /// <summary>
    /// Mammotion robot mowers (Luba, Yuka) through the Mammotion cloud. Account → mower (IoT id, found by scan) → data points
    /// </summary>
    public class MammotionDriverFactory : DriverFactory
    {
        public static readonly Guid MowerInterface = new("937f0785-8d6b-4736-bed0-737840fc3ded");
        public static readonly Guid MowerTemplate = new("00d80904-0db1-4555-9039-2d46b98d1f64");

        public const string UsernameKey = "username";
        public const string PasswordKey = "password";
        public const string PollKey = "poll";
        public const string IotIdKey = "iot_id";

        public override string DriverName => "Mammotion";

        public override Guid DriverGuid => new("bd48c3e0-0453-4632-ad5e-edfdebad1ceb");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.mammotion";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new MammotionDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "MAMMOTION.NAME", "MAMMOTION.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "MAMMOTION.NAME", "MAMMOTION.DESCRIPTION", "mammotion",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("bee03e66-17c2-47a8-a3c8-5a2582e29d0e"), "MAMMOTION.USERNAME.NAME", "MAMMOTION.USERNAME.DESCRIPTION", UsernameKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("5d52adfe-0815-4fa9-8c1a-96cee19d748e"), "MAMMOTION.PASSWORD.NAME", "MAMMOTION.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("16bc8388-8353-4014-8ff4-9aaf23688025"), "MAMMOTION.POLL.NAME", "MAMMOTION.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(30, 3600), 60, 1, 3);
            factory.CreatePropertyTemplate(new Guid("89d4ba15-f244-4a45-81b8-1d722967792f"), "MAMMOTION.SCAN.NAME", "MAMMOTION.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateInterfaceType(MowerInterface, "MAMMOTION.MOWER.NAME", "MAMMOTION.MOWER.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(MowerTemplate, "MAMMOTION.MOWER.NAME", "MAMMOTION.MOWER.DESCRIPTION", "mower", DriverGuid,
                MowerInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("448c880f-4cf0-4007-9311-58954c8d3bdd"), "MAMMOTION.MOWER.IOT_ID.NAME", "MAMMOTION.MOWER.IOT_ID.DESCRIPTION", IotIdKey,
                PropertyTemplateType.Text, MowerTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in MammotionDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"MAMMOTION.MOWER.{dp.I18N}.NAME", $"MAMMOTION.MOWER.{dp.I18N}.DESCRIPTION", dp.Key, MowerInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, !dp.IsAction, true, dp.IsAction, true, dp.Type, 1, false);
            }
        }
    }
}
