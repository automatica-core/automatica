using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using System;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    public class OpenWeatherDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "OpenWeatherMap";

        public override Guid DriverGuid => new Guid("5330ccc5-42b6-4e7e-9c9a-3f53b54cdbe7");

        public override Version DriverVersion => new Version(2, 1, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.open-weather-map";

        public override bool InDevelopmentMode => false;


        public static Guid ForecastDailyAverage = new Guid("61f797c9-6f93-4f70-8f8c-058bb7cb119b");

        public static Guid ForecastDaily = new Guid("8b1f50b6-61f6-4be2-87e7-6727900dd752");
        public static Guid ForecastHourly = new Guid("449a967a-da07-46d5-a902-45ed9936c3a8");

        public static readonly int MaxDailyForecasts = 8;
        public static readonly int MaxHourlyForecasts = 12;
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new OpenWeatherMapDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "OPENWEATHERMAP.NAME", "OPENWEATHERMAP.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "OPENWEATHERMAP.NAME", "OPENWEATHERMAP.DESCRIPTION", "openweathermap", 
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);
           
            factory.CreatePropertyTemplate(new Guid("6fa8e102-3727-4196-80c2-ae764a18aaf0"), "OPENWEATHERMAP.APIKEY.NAME", "OPENWEATHERMAP.APIKEY.DESCRIPTION", "api-key",
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

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


            factory.CreateInterfaceType(ForecastDailyAverage, "OPENWEATHERMAP.DAILY_FORECAST.NAME", "OPENWEATHERMAP.DAILY_FORECAST.DESCRIPTION", int.MaxValue, 1, false);
            factory.CreateNodeTemplate(ForecastDailyAverage, "OPENWEATHERMAP.DAILY_FORECAST.NAME", "OPENWEATHERMAP.DAILY_FORECAST.DESCRIPTION", "openweathermap-forecast-average",
                DriverGuid, ForecastDailyAverage, true, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee7"), "OPENWEATHERMAP.TEMPERATURE_MAX.NAME", "OPENWEATHERMAP.TEMPERATURE_MAX.DESCRIPTION", "openweathermap-forecast-temperature-max",
                ForecastDailyAverage, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(new Guid("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee8"), "OPENWEATHERMAP.TEMPERATURE_MIN.NAME", "OPENWEATHERMAP.TEMPERATURE_MIN.DESCRIPTION", "openweathermap-forecast-temperature-max",
                ForecastDailyAverage, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreateNodeTemplate(new Guid("66d08ddd-550f-4f88-ad22-9d3b8acf8459"), "OPENWEATHERMAP.HUMIDITY.NAME", "OPENWEATHERMAP.HUMIDITY.DESCRIPTION", "openweathermap-forecast-humidity",
                ForecastDailyAverage, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("83c6cc72-b562-4aa5-b284-3689bbbd9988"), "OPENWEATHERMAP.PRESSURE.NAME", "OPENWEATHERMAP.PRESSURE.DESCRIPTION", "openweathermap-forecast-pressure", ForecastDailyAverage,
                           GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("37824dd3-5fd4-4061-895a-30bc0a3f9d95"), "OPENWEATHERMAP.WIND_SPEED.NAME", "OPENWEATHERMAP.WIND_SPEED.DESCRIPTION", "openweathermap-forecast-wind-speed", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("39cdb1e2-a04f-4e35-9dc3-e6261da48dd0"), "OPENWEATHERMAP.WIND_DIRECTION.NAME", "OPENWEATHERMAP.WIND_DIRECTION.DESCRIPTION", "openweathermap-forecast-wind-direction", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("c8889461-67ec-48dd-9789-3204efc74641"), "OPENWEATHERMAP.TEMPERATURE.NAME", "OPENWEATHERMAP.TEMPERATURE.DESCRIPTION", "openweathermap-forecast-temperature", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            
            factory.CreateNodeTemplate(new Guid("24aa4910-5cdd-4f8f-8bbf-6cd7cfbde3c3"), "OPENWEATHERMAP.CLOUDS.NAME", "OPENWEATHERMAP.CLOUDS.DESCRIPTION", "openweathermap-forecast-clouds", ForecastDailyAverage,
                                      GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("fb1d9741-f6ed-433a-94b6-83d40b6000b9"), "OPENWEATHERMAP.CLOUDS_DESC.NAME", "OPENWEATHERMAP.CLOUDS_DESC.DESCRIPTION", "openweathermap-forecast-clouds-description", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("b23f1587-8551-451f-86a7-2947a3592c39"), "OPENWEATHERMAP.PRECIPITATION.NAME", "OPENWEATHERMAP.PRECIPITATION.DESCRIPTION", "openweathermap-forecast-precipitation", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("dc918604-02e3-4caf-8975-35d1742b9e2e"), "OPENWEATHERMAP.PRECIPITATION_DESC.NAME", "OPENWEATHERMAP.PRECIPITATION_DESC.DESCRIPTION", "openweathermap-forecast-precipitation-description", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("691477bc-b5f7-438f-8923-e02168bd886b"), "OPENWEATHERMAP.FORECAST_FROM.NAME", "OPENWEATHERMAP.FORECAST_FROM.DESCRIPTION", "openweathermap-forecast-from", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);

            factory.CreateNodeTemplate(new Guid("0e8a2bdd-dfe7-4a1b-a073-00c0e2aff9ee"), "OPENWEATHERMAP.FORECAST_TO.NAME", "OPENWEATHERMAP.FORECAST_TO.DESCRIPTION", "openweathermap-forecast-to", ForecastDailyAverage,
                            GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);


            CreateDailyForecasts(factory);
            CreateHourlyForecasts(factory);
        }

        private void CreateDailyForecasts(INodeTemplateFactory factory)
        {

            for (var i = 1; i < MaxDailyForecasts; i++)
            {
                var parentGuid = GenerateNewGuid(ForecastDaily, i);
                factory.CreateInterfaceType(parentGuid, $"OPENWEATHERMAP.DAILY_FORECAST_{i}.NAME", "OPENWEATHERMAP.DAILY_FORECAST.DESCRIPTION", int.MaxValue, 1, false);
                factory.CreateNodeTemplate(parentGuid, $"OPENWEATHERMAP.DAILY_FORECAST_{i}.NAME", "OPENWEATHERMAP.DAILY_FORECAST.DESCRIPTION", "openweathermap-forecast-daily",
                    DriverGuid, parentGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);
               
                factory.CreatePropertyTemplate(parentGuid, "INDEX", "INDEX", "openweathermap-forecast-index",
                    PropertyTemplateType.Numeric, parentGuid, "", false, true, null, i, 0, 0);
                

                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("fb84c0a2-ca31-40b2-b653-b61d2d4d45bd"), i), "OPENWEATHERMAP.TEMPERATURE_MAX.NAME", "OPENWEATHERMAP.TEMPERATURE_MAX.DESCRIPTION", "openweathermap-forecast-temperature-max",
                    parentGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("353978cf-2078-4ab7-8383-4e531487f640"), i), "OPENWEATHERMAP.TEMPERATURE_MIN.NAME", "OPENWEATHERMAP.TEMPERATURE_MIN.DESCRIPTION", "openweathermap-forecast-temperature-max",
                    parentGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true, NodeDataType.NoAttribute, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("9d38de94-f1df-4a3c-b3f1-430bc58fa40d"), i), "OPENWEATHERMAP.HUMIDITY.NAME", "OPENWEATHERMAP.HUMIDITY.DESCRIPTION", "openweathermap-forecast-humidity",
                    parentGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("63dd3c59-7779-410a-ab41-220b05a28dea"), i), "OPENWEATHERMAP.PRESSURE.NAME", "OPENWEATHERMAP.PRESSURE.DESCRIPTION", "openweathermap-forecast-pressure", parentGuid,
                               GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("12505ffb-2c97-45bf-b4bc-076d31e31c37"), i), "OPENWEATHERMAP.WIND_SPEED.NAME", "OPENWEATHERMAP.WIND_SPEED.DESCRIPTION", "openweathermap-forecast-wind-speed", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("e9f3bac8-508b-4f54-9d8b-75bf97cfb227"), i), "OPENWEATHERMAP.WIND_DIRECTION.NAME", "OPENWEATHERMAP.WIND_DIRECTION.DESCRIPTION", "openweathermap-forecast-wind-direction", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("bb0edb25-6bfa-4bcb-bdf5-019545ac2179"), i), "OPENWEATHERMAP.TEMPERATURE.NAME", "OPENWEATHERMAP.TEMPERATURE.DESCRIPTION", "openweathermap-forecast-temperature", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("c437094e-bc7e-4f5c-b0d4-7ca487eda7e1"), i), "OPENWEATHERMAP.CLOUDS.NAME", "OPENWEATHERMAP.CLOUDS.DESCRIPTION", "openweathermap-forecast-clouds", parentGuid,
                                          GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("1bb7ba79-4cfd-44fd-9c0e-57540dbd3108"), i), "OPENWEATHERMAP.CLOUDS_DESC.NAME", "OPENWEATHERMAP.CLOUDS_DESC.DESCRIPTION", "openweathermap-forecast-clouds-description", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("a9b75bdc-43fc-4c5c-aa81-a7800bcebcbc"), i), "OPENWEATHERMAP.PRECIPITATION.NAME", "OPENWEATHERMAP.PRECIPITATION.DESCRIPTION", "openweathermap-forecast-precipitation", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Integer, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("4ecac6b7-c0c0-4891-9783-88c78da743f7"), i), "OPENWEATHERMAP.PRECIPITATION_DESC.NAME", "OPENWEATHERMAP.PRECIPITATION_DESC.DESCRIPTION", "openweathermap-forecast-precipitation-description", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("0b584727-88e0-46be-a530-b0c830cc664c"), i), "OPENWEATHERMAP.FORECAST_FROM.NAME", "OPENWEATHERMAP.FORECAST_FROM.DESCRIPTION", "openweathermap-forecast-from", parentGuid,
                                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            }
        }

        private void CreateHourlyForecasts(INodeTemplateFactory factory)
        {

            for (var i = 0; i < MaxHourlyForecasts; i++)
            {
                var parentGuid = GenerateNewGuid(ForecastHourly, i);

                factory.CreateInterfaceType(parentGuid, $"OPENWEATHERMAP.HOURLY_FORECAST_{i}.NAME", "OPENWEATHERMAP.HOURLY_FORECAST.DESCRIPTION", int.MaxValue, 1, false);
                factory.CreateNodeTemplate(parentGuid, $"OPENWEATHERMAP.HOURLY_FORECAST_{i}.NAME", "OPENWEATHERMAP.HOURLY_FORECAST.DESCRIPTION", "openweathermap-forecast-hourly",
                    DriverGuid, parentGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

                factory.CreatePropertyTemplate(parentGuid, "INDEX", "INDEX", "openweathermap-forecast-index",
                    PropertyTemplateType.Numeric, parentGuid, "", false, true, null, i, 0, 0);
              

                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("a7ca37a9-9c45-4d2f-9a8e-b2e0a26ab23f"), i), "OPENWEATHERMAP.TEMPERATURE.NAME", "OPENWEATHERMAP.TEMPERATURE.DESCRIPTION", "openweathermap-forecast-temperature", parentGuid,
               GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("133e5a31-3114-4696-a1a5-1d0e758cc8f0"), i), "OPENWEATHERMAP.TEMPERATURE_PERCEPTION.NAME", "OPENWEATHERMAP.TEMPERATURE_PERCEPTION.DESCRIPTION", "openweathermap-forecast-temperature-perception", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("8195d908-28e9-46ed-85a4-3b659e218f35"), i), "OPENWEATHERMAP.PRESSURE.NAME", "OPENWEATHERMAP.PRESSURE.DESCRIPTION", "openweathermap-forecast-pressure", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("efd2d8a6-852f-41a4-8bf9-ec3116093494"), i), "OPENWEATHERMAP.HUMIDITY.NAME", "OPENWEATHERMAP.HUMIDITY.DESCRIPTION", "openweathermap-forecast-humidity", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("2a5e7721-650e-482e-98d2-c6ca4bc2dbb9"), i), "OPENWEATHERMAP.UV_INDEX.NAME", "OPENWEATHERMAP.UV_INDEX.DESCRIPTION", "openweathermap-forecast-uv-index", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("716daca7-53db-439b-a513-6c89cb32b7ce"), i), "OPENWEATHERMAP.CLOUDS.NAME", "OPENWEATHERMAP.CLOUDS.DESCRIPTION", "openweathermap-forecast-clouds", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("c46d6ece-fd97-4726-8a75-8d63b43e146a"), i), "OPENWEATHERMAP.WIND_SPEED.NAME", "OPENWEATHERMAP.WIND_SPEED.DESCRIPTION", "openweathermap-forecast-wind-speed", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("f0a695f2-030d-4b4e-b0b2-697e8adfc14b"), i), "OPENWEATHERMAP.WIND_DIRECTION.NAME", "OPENWEATHERMAP.WIND_DIRECTION.DESCRIPTION", "openweathermap-forecast-wind-direction", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("80588b18-0343-44c8-b782-f146baf80b65"), i), "OPENWEATHERMAP.PRECIPITATION.NAME", "OPENWEATHERMAP.PRECIPITATION.DESCRIPTION", "openweathermap-forecast-precipitation", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("0c04d651-6ab1-4421-8a31-f9f6d8408e23"), i), "OPENWEATHERMAP.RAIN.NAME", "OPENWEATHERMAP.TEMPERAINRATURE.DESCRIPTION", "openweathermap-forecast-rain", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);
                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("3fa925f0-ee5a-4d08-9646-dbad7535d10d"), i), "OPENWEATHERMAP.SNOW.NAME", "OPENWEATHERMAP.SNOW.DESCRIPTION", "openweathermap-forecast-snow", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false);


                factory.CreateNodeTemplate(GenerateNewGuid(new Guid("39d16e3f-2baa-414b-8a56-5ca150642ed2"), i), "OPENWEATHERMAP.FORECAST_FROM.NAME", "OPENWEATHERMAP.FORECAST_FROM.DESCRIPTION", "openweathermap-forecast-from", parentGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false);
            }
        }


        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }
    }
}
