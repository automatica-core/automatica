using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Times.DriverFactory.Sun;

namespace P3.Driver.Times.DriverFactory
{
    public class SunDriverFactory : BaseTimesDriverFactory
    { 
        public static readonly Guid DriverGuidId = new Guid("78a865b6-9d52-449e-b855-255df86c0316");
        public override Guid DriverGuid => DriverGuidId;
        public override Version DriverVersion => new Version(1, 0, 0, 0);

        public static readonly Guid SunInterfaceGuid = new Guid("124d1808-3e02-4fcf-a3e9-f13ab9511d12");

        public static readonly Guid SunSetGuid = new Guid("55b23d1a-22c1-4b63-bc7e-025197e4e96c");
        public static readonly Guid SunRiseGuid = new Guid("a178ac07-8a52-4439-ab2e-39e8dbb4c21a");
        public static readonly Guid SunIsSetGuid = new Guid("4ce31198-dd47-4e39-a280-a69245e89973");
        public static readonly Guid SunIsRiseGuid = new Guid("e54727b1-3026-4d7c-b63a-48162194a936");

        public static readonly Guid SunDusk = new Guid("241ae3de-2fc1-4d2e-88df-aeee4da6b19e");
        public static readonly Guid SunDawn = new Guid("25e927d7-bbda-4bae-8d1a-159f0eb34c44");
        public static readonly Guid IsDaylight = new Guid("5993a971-0727-46b1-904a-e7d770a0c29f");

        public override string ImageName => "automaticacore/plugin-p3.driver.times";
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            base.InitNodeTemplates(factory);

            factory.CreateInterfaceType(SunInterfaceGuid, "TIMES.SUN.NAME", "TIMES.SUN.NAME", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "TIMES.SUN.NAME", "TIMES.SUN.DESCRIPTION", "sun", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), SunInterfaceGuid, false, true, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(SunSetGuid, "TIMES.SUN.SUNSET.NAME", "TIMES.SUN.SUNSET.DESCRIPTION", "sunset", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(SunRiseGuid, "TIMES.SUN.SUNRISE.NAME", "TIMES.SUN.SUNRISE.DESCRIPTION", "sunrise", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);

            factory.CreateNodeTemplate(SunDusk, "TIMES.SUN.DUSK.NAME", "TIMES.SUN.DUSK.DESCRIPTION", "dusk", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);
            factory.CreateNodeTemplate(SunDawn, "TIMES.SUN.DAWN.NAME", "TIMES.SUN.DAWN.DESCRIPTION", "dawn", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateNodeTemplate(SunIsSetGuid, "TIMES.SUN.IS_SUNSET.NAME", "TIMES.SUN.IS_SUNSET.DESCRIPTION", "is-sunset", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(SunIsRiseGuid, "TIMES.SUN.IS_SUNRISE.NAME", "TIMES.SUN.IS_SUNRISE.DESCRIPTION", "is-sunrise", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(IsDaylight, "TIMES.SUN.IS_DAYLIGHT.NAME", "TIMES.SUN.IS_DAYLIGHT.DESCRIPTION", "is-daylight", SunInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new SunDriver(config);
        }
    }
}
