using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Byd.Driver
{
    internal enum BydSource
    {
        Realtime,
        Gps,
        None
    }

    /// <param name="Read">Value from the realtime or GPS data, null = not readable</param>
    /// <param name="Command">Command for a written value (true / false), null = read only</param>
    internal sealed record BydDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, BydSource Source,
        Func<JsonElement, object> Read = null, Func<bool, string> Command = null)
    {
        public bool IsReadable => Read != null;
        public bool IsWritable => Command != null;
    }

    internal static class BydDataPoints
    {
        private static object Num(JsonElement e, string name)
        {
            var value = BydCloud.Number(e, name);
            return value is null or < 0 ? null : value;
        }

        /// <summary>
        /// true when all doors report locked (2), false when one is unlocked (1), null when nothing is known
        /// </summary>
        internal static object Locked(JsonElement e)
        {
            var states = new[] { "leftFrontDoorLock", "rightFrontDoorLock", "leftRearDoorLock", "rightRearDoorLock" }
                .Select(f => BydCloud.Number(e, f)).Where(v => v is 1 or 2).ToList();
            return states.Count == 0 ? null : states.All(v => v == 2);
        }

        internal static object Charging(JsonElement e)
        {
            var state = BydCloud.Number(e, "chargeState") ?? BydCloud.Number(e, "chargingState");
            return state is null or < 0 ? null : state == 1;
        }

        public static readonly IReadOnlyList<BydDataPoint> All = new[]
        {
            new BydDataPoint(new Guid("9580c9db-c8fa-44ae-93d0-efcf501496d9"), "online", "ONLINE", NodeDataType.Boolean, BydSource.Realtime,
                e => BydCloud.Number(e, "onlineState") switch { 1 => true, 2 => false, _ => null }),
            new BydDataPoint(new Guid("2cc93432-ab60-401f-883d-5b3ffbb10ff7"), "battery", "BATTERY", NodeDataType.Double, BydSource.Realtime, e => Num(e, "elecPercent")),
            new BydDataPoint(new Guid("07d3ec08-5219-4999-a098-56e2732c0508"), "range", "RANGE", NodeDataType.Double, BydSource.Realtime, e => Num(e, "enduranceMileage")),
            new BydDataPoint(new Guid("dcd5816f-dea3-4a96-9180-7b69ca3d74ba"), "odometer", "ODOMETER", NodeDataType.Double, BydSource.Realtime, e => Num(e, "totalMileage")),
            new BydDataPoint(new Guid("3b9ef040-f52d-4256-b0bd-0c429250a063"), "charging", "CHARGING", NodeDataType.Boolean, BydSource.Realtime, Charging),
            new BydDataPoint(new Guid("0a5c3d8e-9054-49cb-8bb1-fd74d4077d3b"), "inside_temp", "INSIDE_TEMP", NodeDataType.Double, BydSource.Realtime,
                e => BydCloud.Number(e, "tempInCar") is { } t && t > -129 ? t : null),
            new BydDataPoint(new Guid("4e5aa325-2c66-4e74-9a81-b2e136e6b4bf"), "speed", "SPEED", NodeDataType.Double, BydSource.Realtime, e => Num(e, "speed")),
            new BydDataPoint(new Guid("6efdb660-678e-4045-b128-44d8021d861b"), "locked", "LOCKED", NodeDataType.Boolean, BydSource.Realtime, Locked,
                on => on ? "LOCKDOOR" : "OPENDOOR"),
            new BydDataPoint(new Guid("1510de92-1854-48a9-832b-abb57181aa44"), "climate", "CLIMATE", NodeDataType.Boolean, BydSource.None, null,
                on => on ? "OPENAIR" : "CLOSEAIR"),
            new BydDataPoint(new Guid("787f181d-f25f-4369-a816-8f9c32d94c75"), "latitude", "LATITUDE", NodeDataType.Double, BydSource.Gps, e => BydCloud.Number(e, "latitude")),
            new BydDataPoint(new Guid("ab8f058b-7751-4ebb-8b8a-005e581ab005"), "longitude", "LONGITUDE", NodeDataType.Double, BydSource.Gps, e => BydCloud.Number(e, "longitude")),
            new BydDataPoint(new Guid("663da5b0-4841-45f9-9d00-fa9b780a10bd"), "flash_lights", "FLASH_LIGHTS", NodeDataType.Boolean, BydSource.None, null,
                on => on ? "FLASHLIGHTNOWHISTLE" : null),
            new BydDataPoint(new Guid("b0f6490a-24e6-4fd4-af62-472ef8edefca"), "find_car", "FIND_CAR", NodeDataType.Boolean, BydSource.None, null,
                on => on ? "FINDCAR" : null),
        };
    }
}
