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

        public override Version DriverVersion => new Version(1, 2, 0, 1);

        public override string ImageName => "automaticacore/plugin-p3.driver.open-weather-map";

        public override bool InDevelopmentMode => false;


        public static Guid Forecast = new Guid("61f797c9-6f93-4f70-8f8c-058bb7cb119b");
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
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("2638083b-7db9-4a18-9b1f-41c1b1deb15f"), "OPENWEATHERMAP.POLL_INTERVAL.NAME", "OPENWEATHERMAP.POLL_INTERVAL.DESCRIPTION", "poll",
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(TimeSpan.FromMinutes(15).TotalSeconds, TimeSpan.FromHours(2).TotalSeconds), TimeSpan.FromMinutes(15).TotalSeconds, 1, 1);
            factory.CreatePropertyTemplate(new Guid("a5c0e5a8-75b6-4a91-957a-f630d5026a6e"), "OPENWEATHERMAP.FORECAST_COUNT.NAME", "OPENWEATHERMAP.FORECAST_COUNT.DESCRIPTION", "forecast_count",
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(2, 10), 3, 1, 2);


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


            factory.CreateInterfaceType(Forecast, "OPENWEATHERMAP.FORECAST.NAME", "OPENWEATHERMAP.FORECAST.DESCRIPTION", int.MaxValue, 1, false);
            factory.CreateNodeTemplate(Forecast, "OPENWEATHERMAP.FORECAST.NAME", "OPENWEATHERMAP.FORECAST.DESCRIPTION", "openweathermap-forecast",
                DriverGuid, Forecast, true, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee7"), "OPENWEATHERMAP.TEMPERATURE_MAX.NAME", "OPENWEATHERMAP.TEMPERATURE_MAX.DESCRIPTION", "openweathermap-forecast-temperature-max",
                DriverGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(new Guid("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee8"), "OPENWEATHERMAP.TEMPERATURE_MIN.NAME", "OPENWEATHERMAP.TEMPERATURE_MIN.DESCRIPTION", "openweathermap-forecast-temperature-max",
                DriverGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("66d08ddd-550f-4f88-ad22-9d3b8acf8459"), "OPENWEATHERMAP.HUMIDITY.NAME", "OPENWEATHERMAP.HUMIDITY.DESCRIPTION", "openweathermap-forecast-humidity",
                Forecast, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("83c6cc72-b562-4aa5-b284-3689bbbd9988"), "OPENWEATHERMAP.PRESSURE.NAME", "OPENWEATHERMAP.PRESSURE.DESCRIPTION", "openweathermap-forecast-pressure", Forecast,
                           GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("37824dd3-5fd4-4061-895a-30bc0a3f9d95"), "OPENWEATHERMAP.WIND_SPEED.NAME", "OPENWEATHERMAP.WIND_SPEED.DESCRIPTION", "openweathermap-forecast-wind-speed", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("39cdb1e2-a04f-4e35-9dc3-e6261da48dd0"), "OPENWEATHERMAP.WIND_DIRECTION.NAME", "OPENWEATHERMAP.WIND_DIRECTION.DESCRIPTION", "openweathermap-forecast-wind-direction", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("c8889461-67ec-48dd-9789-3204efc74641"), "OPENWEATHERMAP.TEMPERATURE.NAME", "OPENWEATHERMAP.TEMPERATURE.DESCRIPTION", "openweathermap-forecast-temperature", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            
            factory.CreateNodeTemplate(new Guid("24aa4910-5cdd-4f8f-8bbf-6cd7cfbde3c3"), "OPENWEATHERMAP.CLOUDS.NAME", "OPENWEATHERMAP.CLOUDS.DESCRIPTION", "openweathermap-forecast-clouds", Forecast,
                                      GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("fb1d9741-f6ed-433a-94b6-83d40b6000b9"), "OPENWEATHERMAP.CLOUDS_DESC.NAME", "OPENWEATHERMAP.CLOUDS_DESC.DESCRIPTION", "openweathermap-forecast-clouds-description", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("b23f1587-8551-451f-86a7-2947a3592c39"), "OPENWEATHERMAP.PRECIPITATION.NAME", "OPENWEATHERMAP.PRECIPITATION.DESCRIPTION", "openweathermap-forecast-precipitation", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("dc918604-02e3-4caf-8975-35d1742b9e2e"), "OPENWEATHERMAP.PRECIPITATION_DESC.NAME", "OPENWEATHERMAP.PRECIPITATION_DESC.DESCRIPTION", "openweathermap-forecast-precipitation-description", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("691477bc-b5f7-438f-8923-e02168bd886b"), "OPENWEATHERMAP.FORECAST_FROM.NAME", "OPENWEATHERMAP.FORECAST_FROM.DESCRIPTION", "openweathermap-forecast-from", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);

            factory.CreateNodeTemplate(new Guid("0e8a2bdd-dfe7-4a1b-a073-00c0e2aff9ee"), "OPENWEATHERMAP.FORECAST_TO.NAME", "OPENWEATHERMAP.FORECAST_TO.DESCRIPTION", "openweathermap-forecast-to", Forecast,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);

        }
    }
}
