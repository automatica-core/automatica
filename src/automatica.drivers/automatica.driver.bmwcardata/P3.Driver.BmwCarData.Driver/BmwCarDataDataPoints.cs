using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.BmwCarData.Driver
{
    /// <summary>
    /// A read-only vehicle value: the first of its telematic keys that has a value wins
    /// </summary>
    internal sealed record BmwDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, IReadOnlyList<string> Descriptors, bool DefaultCreated = true)
    {
        public bool IsCustom => Descriptors.Count == 0;
    }

    internal static class BmwCarDataDataPoints
    {
        public const string CustomNumberKey = "custom_number";
        public const string CustomTextKey = "custom_text";
        public const string LastUpdateKey = "last_update";

        private static string[] K(params string[] keys) => keys;

        /// <summary>
        /// Keys from the BMW integration guide (location) and from evcc's CarData integration (the rest). Further keys
        /// from the Telematics Data Catalogue can be added as custom values.
        /// </summary>
        public static readonly IReadOnlyList<BmwDataPoint> All = new List<BmwDataPoint>
        {
            new(new Guid("287298ba-390a-401b-941f-0481e5961555"), "soc", "SOC", NodeDataType.Double,
                K("vehicle.drivetrain.batteryManagement.header", "vehicle.powertrain.electric.battery.stateOfCharge.displayed")),
            new(new Guid("8c95e88e-8560-4566-b696-5eefbd08f8d6"), "target_soc", "TARGET_SOC", NodeDataType.Double, K("vehicle.powertrain.electric.battery.stateOfCharge.target")),
            new(new Guid("69f78f99-0a5e-408e-acca-f94d0daa812b"), "electric_range", "ELECTRIC_RANGE", NodeDataType.Double, K("vehicle.drivetrain.electricEngine.kombiRemainingElectricRange")),
            new(new Guid("0c801fd6-f858-40c6-879a-7ec3b4a5c0e5"), "range", "RANGE", NodeDataType.Double, K("vehicle.drivetrain.lastRemainingRange")),
            new(new Guid("4f1c04a5-1afa-4665-bc17-042c4444a55f"), "mileage", "MILEAGE", NodeDataType.Double, K("vehicle.vehicle.travelledDistance")),
            new(new Guid("687ad00f-769f-42c5-bd8c-5077682ed24b"), "charging_status", "CHARGING_STATUS", NodeDataType.String, K("vehicle.drivetrain.electricEngine.charging.status")),
            new(new Guid("4098a7df-cae8-4f16-b102-99b4c2a39d5f"), "charging_time_remaining", "CHARGING_TIME_REMAINING", NodeDataType.Double, K("vehicle.drivetrain.electricEngine.charging.timeRemaining")),
            new(new Guid("f661fc9b-244b-4b29-8ac5-226198d8d778"), "charging_port", "CHARGING_PORT", NodeDataType.String, K("vehicle.body.chargingPort.status")),
            new(new Guid("dd4dcea8-3b45-4f04-ba3e-c672f82afc5a"), "latitude", "LATITUDE", NodeDataType.Double, K("vehicle.cabin.infotainment.navigation.currentLocation.latitude")),
            new(new Guid("04ae82ce-7b25-471b-a9e2-5a35327b3076"), "longitude", "LONGITUDE", NodeDataType.Double, K("vehicle.cabin.infotainment.navigation.currentLocation.longitude")),
            new(new Guid("d6a2616f-3cb1-401a-ad2b-49eeb70bf523"), LastUpdateKey, "LAST_UPDATE", NodeDataType.DateTime, K()),
            new(new Guid("28c044b8-71e1-4e80-a655-8d574040e30c"), CustomNumberKey, "CUSTOM_NUMBER", NodeDataType.Double, K(), false),
            new(new Guid("9fa03f01-fed6-4bdd-b865-ede498987192"), CustomTextKey, "CUSTOM_TEXT", NodeDataType.String, K(), false),
        };

        public static bool IsCustom(string key) => key is CustomNumberKey or CustomTextKey;

        public static IEnumerable<string> StandardDescriptors => All.SelectMany(d => d.Descriptors);

        /// <summary>
        /// REST values are strings, stream values may be numbers or booleans
        /// </summary>
        public static object Convert(object value, NodeDataType type)
        {
            if (value == null)
            {
                return null;
            }
            switch (type)
            {
                case NodeDataType.Double:
                    if (value is int or long or double)
                    {
                        return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    }
                    return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
                case NodeDataType.Boolean:
                    return value is bool b ? b : bool.TryParse(value.ToString(), out var parsed) ? parsed : null;
                default:
                    return value is bool flag ? (flag ? "true" : "false") : System.Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
