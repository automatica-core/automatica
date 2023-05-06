using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Times.DriverFactory.DateTime;

namespace P3.Driver.Times.DriverFactory
{
    public class DateTimeDriverFactory : BaseTimesDriverFactory
    {
        public static readonly Guid DriverGuidId = new Guid("7bda80c5-ef7f-41ea-b637-40718589bb9e");
        public override Version DriverVersion => new Version(1, 1, 0, 0);
        public override Guid DriverGuid => DriverGuidId;

        public static readonly Guid DateTimeInterfaceGuid = new Guid("15ff46f8-e433-4707-913d-4ed9dac98d17");
        public static readonly Guid DateTime = new Guid("5a7bb474-1965-486a-8900-6fe66d68db7f");
        public static readonly Guid Date = new Guid("a507f35a-40b7-48d8-81c0-8bda55b0aba4");
        public static readonly Guid Time = new Guid("cf38f5d6-9d5c-4aee-bdcb-a24e0a445618");

        public static readonly Guid DayOfWeek = new Guid("22e03f5b-3243-4379-bd05-19d2d94e6dcd");
        public static readonly Guid DayOfMonth = new Guid("f8b317b6-ee4b-446a-abe0-629a658e0c0a");
        public static readonly Guid Month = new Guid("09e920d3-ae53-4375-b324-f18e75316302");
        public static readonly Guid Year = new Guid("de27c4d2-1c5a-402a-9322-8fe65de32d81");


        public static readonly Guid Milliseconds = new Guid("ecd03ad7-423e-474f-8dbc-97674d1b49ef");
        public static readonly Guid Seconds = new Guid("c4ff9873-0d29-4ecf-9afa-3fe0f22a1683");
        public static readonly Guid Minutes = new Guid("395943f3-8278-4301-9f26-1e52029abc53");
        public static readonly Guid Hours = new Guid("6cf1c2ca-a5e8-47ac-9ccd-1def90f45790");



        public static readonly Guid BootDateTime = new Guid("8b3a0307-34dc-4ad5-b18e-792ee2f86113");
        public static readonly Guid SecondsSinceBoot = new Guid("f4a6fe61-bbff-4da9-87d5-9b97e9b0037a");

        public override string ImageName => "automaticacore/plugin-p3.driver.times";

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            base.InitNodeTemplates(factory);


            factory.CreateInterfaceType(DateTimeInterfaceGuid, "TIMES.INTERFACE.NAME", "TIMES.INTERFACE.NAME", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "TIMES.DATETIME.NAME", "TIMES.DATETIME.DESCRIPTION", "times", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DateTimeInterfaceGuid, false, true, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(DateTime, "TIMES.DATETIME.DATETIME.NAME", "TIMES.DATETIME.DATETIME.DESCRIPTION", "times-datetime", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            factory.CreateNodeTemplate(Date, "TIMES.DATETIME.DATE.NAME", "TIMES.DATETIME.DATE.DESCRIPTION", "times-date", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
            factory.CreateNodeTemplate(Time, "TIMES.DATETIME.TIME.NAME", "TIMES.DATETIME.TIME.DESCRIPTION", "times-time", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Time, 1, false);
            factory.ChangeDefaultVisuTemplate(Time, VisuMobileObjectTemplateTypes.Clock);

            factory.CreateNodeTemplate(DayOfWeek, "TIMES.DATETIME.DAY_OF_WEEK.NAME", "TIMES.DATETIME.DAY_OF_WEEK.DESCRIPTION", "times-day-of-week", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(DayOfMonth, "TIMES.DATETIME.DAY_OF_MONTH.NAME", "TIMES.DATETIME.DAY_OF_MONTH.DESCRIPTION", "times-day-of-month", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(Month, "TIMES.DATETIME.MONTH.NAME", "TIMES.DATETIME.MONTH.DESCRIPTION", "times-month", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(Year, "TIMES.DATETIME.YEAR.NAME", "TIMES.DATETIME.YEAR.DESCRIPTION", "times-year", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(Milliseconds, "TIMES.DATETIME.MILLISECONDS.NAME", "TIMES.DATETIME.MILLISECONDS.DESCRIPTION", "times-milliseconds", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(Seconds, "TIMES.DATETIME.SECONDS.NAME", "TIMES.DATETIME.SECONDS.DESCRIPTION", "times-seconds", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(Minutes, "TIMES.DATETIME.MINUTES.NAME", "TIMES.DATETIME.MINUTES.DESCRIPTION", "times-minutes", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(Hours, "TIMES.DATETIME.HOURS.NAME", "TIMES.DATETIME.HOURS.DESCRIPTION", "times-hours", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(SecondsSinceBoot, "TIMES.DATETIME.SECONDS_SINCE_BOOT.NAME", "TIMES.DATETIME.SECONDS_SINCE_BOOT.DESCRIPTION", "times-running", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
            factory.CreateNodeTemplate(BootDateTime, "TIMES.DATETIME.BOOT_TIME.NAME", "TIMES.DATETIME.BOOT_TIME.DESCRIPTION", "boot-time", DateTimeInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new DateTimeDriver(config);
        }
    }
}
