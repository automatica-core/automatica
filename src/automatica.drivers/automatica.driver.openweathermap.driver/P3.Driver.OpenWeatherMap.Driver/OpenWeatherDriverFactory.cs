using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using System;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    public class OpenWeatherDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "OpenWatherMap";

        public override Guid DriverGuid => new Guid("5330ccc5-42b6-4e7e-9c9a-3f53b54cdbe7");

        public override Version DriverVersion => new Version(0, 3, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.open-weather-map";

        public override bool InDevelopmentMode => false;
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new OpenWeatherMapDriver(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>9
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "OPENWEATHERMAP.NAME", "OPENWEATHERMAP.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "OPENWEATHERMAP.NAME", "OPENWEATHERMAP.DESCRIPTION", "openweathermap", 
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);
            factory.CreatePropertyTemplate(new Guid("6fa8e102-3727-4196-80c2-ae764a18aaf0"), "OPENWEATHERMAP.APIKEY.NAME", "OPENWEATHERMAP.APIKEY.DESCRIPTION", "api-key",
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADRESS", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("2638083b-7db9-4a18-9b1f-41c1b1deb15f"), "OPENWEATHERMAP.POLL_INTERVAL.NAME", "OPENWEATHERMAP.POLL_INTERVAL.DESCRIPTION", "poll",
              PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADRESS", true, false, PropertyHelper.CreateRangeMetaString(TimeSpan.FromMinutes(5).TotalSeconds, TimeSpan.FromHours(2).TotalSeconds), TimeSpan.FromMinutes(5).TotalSeconds, 1, 1);


            factory.CreateNodeTemplate(new Guid("6f9173ee-2f3f-4d77-afdd-a980ff364639"), "OPENWEATHERMAP.SUNRISE.NAME", "OPENWEATHERMAP.SUNRISE.DESCRIPTION", "openweathermap-sunrise", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("db6a7d07-a3fa-4fb1-a470-4101c16be04c"), "OPENWEATHERMAP.SUNSET.NAME", "OPENWEATHERMAP.SUNSET.DESCRIPTION", "openweathermap-sunset", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("5870e3de-4fbe-49e1-b051-55a427eebd44"), "OPENWEATHERMAP.HUMIDITY.NAME", "OPENWEATHERMAP.HUMIDITY.DESCRIPTION", "openweathermap-humidity", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("c148157e-9b8f-449e-a0ee-6ccd8b2d70b8"), "OPENWEATHERMAP.PRESSURE.NAME", "OPENWEATHERMAP.PRESSURE.DESCRIPTION", "openweathermap-pressure", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("2b052303-7be3-4b06-96bb-7da334693db8"), "OPENWEATHERMAP.WIND_SPEED.NAME", "OPENWEATHERMAP.WIND_SPEED.DESCRIPTION", "openweathermap-wind-speed", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("fe13ca1b-b04f-4218-b69c-d744a7f7d9fe"), "OPENWEATHERMAP.WIND_DIRECTION.NAME", "OPENWEATHERMAP.WIND_DIRECTION.DESCRIPTION", "openweathermap-wind-direction", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("c0dcd6d9-673b-4c8c-86c3-3b19790b112d"), "OPENWEATHERMAP.TEMPERATURE.NAME", "OPENWEATHERMAP.TEMPERATURE.DESCRIPTION", "openweathermap-temperature", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("f879f1f8-f60c-466d-a307-d26e6314bb37"), "OPENWEATHERMAP.TEMPERATURE_MAX.NAME", "OPENWEATHERMAP.TEMPERATURE_MAX.DESCRIPTION", "openweathermap-temperature-max", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("00abd2ee-5f6b-4dd9-a0a1-b7011f3c0bdd"), "OPENWEATHERMAP.TEMPERATURE_MIN.NAME", "OPENWEATHERMAP.TEMPERATURE_MIN.DESCRIPTION", "openweathermap-temperature-min", DriverGuid,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

        }
    }
}
