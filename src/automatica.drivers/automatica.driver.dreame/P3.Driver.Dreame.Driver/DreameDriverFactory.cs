using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Dreame.Driver
{
    /// <summary>
    /// Dreame robot vacuums through the Dreamehome cloud. Account → vacuum (device id, found by scan) → data points
    /// </summary>
    public class DreameDriverFactory : DriverFactory
    {
        public static readonly Guid VacuumInterface = new("25a93f51-863b-4c2d-bb0f-f86a48dc2f57");
        public static readonly Guid VacuumTemplate = new("12b4d0f7-fdc5-4919-ab68-2fd65826baf0");

        public const string RegionKey = "region";
        public const string UsernameKey = "username";
        public const string PasswordKey = "password";
        public const string PollKey = "poll";
        public const string DidKey = "did";

        public override string DriverName => "Dreame";

        public override Guid DriverGuid => new("d55bc5c2-0107-4723-8490-d97561d8750a");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.dreame";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new DreameDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "DREAME.NAME", "DREAME.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "DREAME.NAME", "DREAME.DESCRIPTION", "dreame",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("8acb8ed3-8b69-44ef-be63-dd96d8b3f157"), "DREAME.REGION.NAME", "DREAME.REGION.DESCRIPTION", RegionKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "eu", 1, 1);
            factory.CreatePropertyTemplate(new Guid("d5d1e7c2-9965-4c04-af0c-631198cce8e2"), "DREAME.USERNAME.NAME", "DREAME.USERNAME.DESCRIPTION", UsernameKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("65f48c5f-acaa-4414-888e-517d8bebe972"), "DREAME.PASSWORD.NAME", "DREAME.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("fb4e0504-6619-4e27-86e6-e05e5944ab24"), "DREAME.POLL.NAME", "DREAME.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(15, 3600), 60, 1, 4);
            factory.CreatePropertyTemplate(new Guid("be3c59ff-5fc5-4b1f-9109-1af697533de5"), "DREAME.SCAN.NAME", "DREAME.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateInterfaceType(VacuumInterface, "DREAME.VACUUM.NAME", "DREAME.VACUUM.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(VacuumTemplate, "DREAME.VACUUM.NAME", "DREAME.VACUUM.DESCRIPTION", "vacuum", DriverGuid,
                VacuumInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("1e5fd895-5d29-47a1-a8c6-e9c0093b2041"), "DREAME.VACUUM.DID.NAME", "DREAME.VACUUM.DID.DESCRIPTION", DidKey,
                PropertyTemplateType.Text, VacuumTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in DreameDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"DREAME.VACUUM.{dp.I18N}.NAME", $"DREAME.VACUUM.{dp.I18N}.DESCRIPTION", dp.Key, VacuumInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.Access != DreameAccess.Action, true,
                    dp.Access != DreameAccess.Read, true, dp.Type, 1, false);
            }
        }
    }
}
