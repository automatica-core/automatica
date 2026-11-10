using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Byd.Driver
{
    /// <summary>
    /// BYD cars through the BYD app cloud (overseas). Account → vehicle (VIN, found by scan) → data points
    /// </summary>
    public class BydDriverFactory : DriverFactory
    {
        public static readonly Guid VehicleInterface = new("6c06ad58-5daa-4143-9117-84d63b2f6130");
        public static readonly Guid VehicleTemplate = new("126ce8e2-38d6-49c0-b685-1a9ccd90a2cf");

        public const string ApiUrlKey = "api_url";
        public const string UsernameKey = "username";
        public const string PasswordKey = "password";
        public const string PinKey = "pin";
        public const string CountryKey = "country";
        public const string PollKey = "poll";
        public const string VinKey = "vin";
        public const string ClimateTemperatureKey = "climate_temperature";
        public const string ClimateDurationKey = "climate_duration";

        public override string DriverName => "BYD";

        public override Guid DriverGuid => new("36a22438-adc1-44d4-a9cb-41e671741f19");

        // 1.1: data points charge_power and time_to_full
        public override Version DriverVersion => new(1, 1, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.byd";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new BydDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "BYD.NAME", "BYD.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "BYD.NAME", "BYD.DESCRIPTION", "byd",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("952f62b9-869c-4a64-8cd7-56f3f28ec2f7"), "BYD.USERNAME.NAME", "BYD.USERNAME.DESCRIPTION", UsernameKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("e89bd310-7ef3-4c0d-bc8e-14262c06ac93"), "BYD.PASSWORD.NAME", "BYD.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("c20e3668-82cb-4432-913b-85806f85532a"), "BYD.PIN.NAME", "BYD.PIN.DESCRIPTION", PinKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("f89a20cd-ff04-4e0d-b919-7a9580a67cd7"), "BYD.COUNTRY.NAME", "BYD.COUNTRY.DESCRIPTION", CountryKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "DE", 1, 4);
            factory.CreatePropertyTemplate(new Guid("e679527d-cd14-4e18-9878-93c062079141"), "BYD.POLL.NAME", "BYD.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(120, 3600), 600, 1, 5);
            factory.CreatePropertyTemplate(new Guid("dd10c605-5c5c-4479-80c8-e04cc59eed03"), "BYD.API_URL.NAME", "BYD.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, BydCloud.DefaultBaseUrl, 1, 6);
            factory.CreatePropertyTemplate(new Guid("196330f7-b37b-4bd6-ba5c-c8d0801ee718"), "BYD.SCAN.NAME", "BYD.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateInterfaceType(VehicleInterface, "BYD.VEHICLE.NAME", "BYD.VEHICLE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(VehicleTemplate, "BYD.VEHICLE.NAME", "BYD.VEHICLE.DESCRIPTION", "vehicle", DriverGuid,
                VehicleInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("60fb5f10-0d47-482c-b743-4699cdd03f25"), "BYD.VEHICLE.VIN.NAME", "BYD.VEHICLE.VIN.DESCRIPTION", VinKey,
                PropertyTemplateType.Text, VehicleTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("69723a83-9bf4-420b-9eca-0761e8d19c36"), "BYD.VEHICLE.CLIMATE_TEMPERATURE.NAME", "BYD.VEHICLE.CLIMATE_TEMPERATURE.DESCRIPTION", ClimateTemperatureKey,
                PropertyTemplateType.Range, VehicleTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(15, 31), 21, 1, 2);
            factory.CreatePropertyTemplate(new Guid("d70315e7-ced0-488a-a8fe-61d241838b3e"), "BYD.VEHICLE.CLIMATE_DURATION.NAME", "BYD.VEHICLE.CLIMATE_DURATION.DESCRIPTION", ClimateDurationKey,
                PropertyTemplateType.Range, VehicleTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(10, 30), 10, 1, 3);

            foreach (var dp in BydDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"BYD.VEHICLE.{dp.I18N}.NAME", $"BYD.VEHICLE.{dp.I18N}.DESCRIPTION", dp.Key, VehicleInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.IsReadable || dp.Key == "climate", true, dp.IsWritable, true, dp.Type, 1, false);
            }
        }
    }
}
