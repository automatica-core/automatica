#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Driver.Shelly.CoIoT;
using Automatica.Driver.Shelly.Gen1;
using Automatica.Driver.Shelly.Rpc;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.ShellyFactory.Generic
{
    internal sealed record ShellyCredentials(string? UserName, string? Password);

    /// <summary>
    /// Learns a device: reads what it is (GET /shelly), its status (and config / settings) and builds the device node
    /// with exactly the components and data points the device has.
    /// </summary>
    internal static class ShellyNodeBuilder
    {
        /// <summary>
        /// Reads the device at <paramref name="address"/> and builds its node tree, null when it is no Shelly or not reachable.
        /// </summary>
        public static async Task<NodeInstance?> ProbeAsync(INodeTemplateFactory factory, string address, ShellyCredentials credentials, ILogger logger,
            CancellationToken token = default, TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
            JObject info;
            try
            {
                // /shelly needs no login on any generation
                using var plain = new ShellyGen1HttpClient(address, null, null, null, effectiveTimeout);
                info = await plain.GetJsonAsync("/shelly", token) as JObject ?? new JObject();
            }
            catch (ShellyRpcException e)
            {
                logger.LogInformation($"No shelly at {address}: {e.Message}");
                return null;
            }

            if (info["mac"] == null)
            {
                logger.LogInformation($"{address} answered /shelly without a mac, it is no shelly");
                return null;
            }

            try
            {
                var generation = info.Value<int?>("gen") ?? 1;
                if (generation >= 2)
                {
                    using var rpc = new ShellyRpcClient(address, credentials.Password, logger, null, effectiveTimeout);
                    var status = await rpc.CallAsync("Shelly.GetStatus", null, token) as JObject ?? new JObject();
                    JObject? config = null;
                    try
                    {
                        config = await rpc.CallAsync("Shelly.GetConfig", null, token) as JObject;
                    }
                    catch (ShellyRpcException e)
                    {
                        logger.LogDebug($"No config of {address}: {e.Message}");
                    }

                    return BuildRpc(factory, info, status, config, address, credentials.Password);
                }

                using var gen1 = new ShellyGen1HttpClient(address, credentials.UserName, credentials.Password, null, effectiveTimeout);
                var gen1Status = await gen1.GetJsonAsync("/status", token) as JObject ?? new JObject();
                JObject? settings = null;
                string? description = null;
                try
                {
                    settings = await gen1.GetJsonAsync("/settings", token) as JObject;
                }
                catch (ShellyRpcException e)
                {
                    logger.LogDebug($"No settings of {address}: {e.Message}");
                }

                try
                {
                    description = (await gen1.GetJsonAsync("/cit/d", token, TimeSpan.FromSeconds(3)) as JObject)?.ToString(Newtonsoft.Json.Formatting.None);
                }
                catch (ShellyRpcException e)
                {
                    logger.LogDebug($"No CoIoT description of {address}: {e.Message}");
                }

                return BuildGen1(factory, info, gen1Status, settings, description, address, credentials);
            }
            catch (ShellyRpcException e)
            {
                logger.LogWarning($"Could not read shelly {info.Value<string>("mac")} at {address}: {e.Message}");
                return null;
            }
        }

        public static NodeInstance BuildRpc(INodeTemplateFactory factory, JObject info, JObject status, JObject? config, string? address, string? password)
        {
            var device = factory.CreateNodeInstance(ShellyGenericTemplates.RpcDeviceTemplate);
            var mac = ShellyGenericDevice.NormalizeMac(info.Value<string>("mac") ?? MacFromId(info.Value<string>("id")));
            var model = info.Value<string>("model") ?? "";
            var app = info.Value<string>("app") ?? model;

            device.Name = FirstNonEmpty(info.Value<string>("name"), config?.SelectToken("sys.device.name")?.Value<string>(), $"Shelly {app} {Suffix(mac)}");
            device.SetProperty(ShellyFactory.DeviceIdPropertyKey, mac);
            device.SetProperty(ShellyFactory.DeviceIpPropertyKey, address ?? "");
            device.SetProperty(ShellyGenericTemplates.PasswordKey, password ?? "");
            device.SetProperty(ShellyGenericTemplates.ModelKey, string.IsNullOrEmpty(model) ? app : $"{model} ({app})");
            device.SetProperty(ShellyGenericTemplates.GenerationKey, info.Value<int?>("gen") ?? 2);
            device.SetProperty(ShellyGenericTemplates.SleepingKey, IsSleepingRpc(status));
            device.SetProperty(ShellyGenericTemplates.PollingIntervalKey, ShellyGenericTemplates.RpcDefaultPolling);

            var bthomeNames = BtHomeDeviceNames(config);

            foreach (var type in ShellyRpcCatalog.All)
            {
                foreach (var id in type.InstanceIds(status).OrderBy(a => a))
                {
                    var componentStatus = type.Select(status, id);
                    var componentConfig = config?[type.Key(id)] as JObject;
                    var name = ComponentName(type, id, componentConfig, bthomeNames);
                    AddComponent(factory, device, type, id, componentStatus, status, name);
                }
            }

            return device;
        }

        public static NodeInstance BuildGen1(INodeTemplateFactory factory, JObject info, JObject status, JObject? settings, string? coiotDescription,
            string? address, ShellyCredentials credentials)
        {
            var device = factory.CreateNodeInstance(ShellyGenericTemplates.Gen1DeviceTemplate);
            var mac = ShellyGenericDevice.NormalizeMac(info.Value<string>("mac"));
            var type = info.Value<string>("type") ?? settings?.SelectToken("device.type")?.Value<string>() ?? "";
            var mode = settings?.Value<string>("mode") ?? "";
            // /shelly says sleep_mode on battery devices; the settings of a sleeping H&T / Flood have a sleep period object
            // (Motion and TRV keep WiFi on and have none)
            var sleeping = info.Value<bool?>("sleep_mode") ?? settings?["sleep_mode"] is JObject;

            device.Name = FirstNonEmpty(settings?.Value<string>("name"), $"Shelly {type} {Suffix(mac)}");
            device.SetProperty(ShellyFactory.DeviceIdPropertyKey, mac);
            device.SetProperty(ShellyFactory.DeviceIpPropertyKey, address ?? "");
            device.SetProperty(ShellyGenericTemplates.UserNameKey, credentials.UserName ?? "");
            device.SetProperty(ShellyGenericTemplates.PasswordKey, credentials.Password ?? "");
            device.SetProperty(ShellyGenericTemplates.ModelKey, type);
            device.SetProperty(ShellyGenericTemplates.GenerationKey, 1);
            device.SetProperty(ShellyGenericTemplates.ModeKey, mode);
            device.SetProperty(ShellyGenericTemplates.SleepingKey, sleeping);
            device.SetProperty(ShellyGenericTemplates.CoIoTDescriptionKey, coiotDescription ?? "");
            device.SetProperty(ShellyGenericTemplates.PollingIntervalKey, ShellyGenericTemplates.Gen1DefaultPolling);

            foreach (var componentType in ShellyGen1Catalog.All)
            {
                // in roller mode the 2.5 still lists both relays in /status, they are not usable then
                if (componentType.Type == "relays" && mode == "roller")
                {
                    continue;
                }

                if (componentType.Type == "rollers" && mode == "relay")
                {
                    continue;
                }

                foreach (var id in componentType.InstanceIds(status))
                {
                    var componentStatus = componentType.Select(status, id);
                    var configName = componentType.Layout == ShellyComponentLayout.Array
                        ? (settings?[componentType.Type] as JArray)?.ElementAtOrDefault(id)?.Value<string>("name")
                        : null;
                    AddComponent(factory, device, componentType, id, componentStatus, status, configName);
                }
            }

            return device;
        }

        private static void AddComponent(INodeTemplateFactory factory, NodeInstance device, ShellyComponentType type, int id, JToken? componentStatus,
            JObject deviceStatus, string? name)
        {
            var included = type.DataPoints.Where(dp => dp.ShouldInclude(componentStatus, deviceStatus)).ToList();
            if (included.Count == 0 || included.All(a => a.IsCommand))
            {
                return;
            }

            var component = factory.CreateNodeInstance(type.Template);
            if (type.HasId)
            {
                component.SetProperty(ShellyGenericTemplates.ComponentIdKey, id);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                component.Name = name;
            }
            else if (type.HasId)
            {
                component.Name = $"{component.Name} {id}";
            }

            // the real template factory creates every data point (default created), the scan keeps only the ones the device has
            component.InverseThis2ParentNodeInstanceNavigation.Clear();
            foreach (var dp in included)
            {
                var child = factory.CreateNodeInstance(dp.Template);
                child.This2ParentNodeInstanceNavigation = component;
                child.This2ParentNodeInstance = component.ObjId;
                component.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }

            component.This2ParentNodeInstanceNavigation = device;
            component.This2ParentNodeInstance = device.ObjId;
            device.InverseThis2ParentNodeInstanceNavigation.Add(component);
        }

        /// <summary>
        /// Battery Gen2+ devices report sys.wakeup_period / wakeup_reason and a devicepower component without external power.
        /// </summary>
        public static bool IsSleepingRpc(JObject status)
        {
            var sys = status["sys"] as JObject;
            if (sys?["wakeup_period"] != null || sys?["wakeup_reason"] != null)
            {
                return true;
            }

            return false;
        }

        private static string? ComponentName(ShellyComponentType type, int id, JObject? config, IReadOnlyDictionary<string, string> bthomeNames)
        {
            var name = config?.Value<string>("name");
            if (type.Type == "bthomedevice" && string.IsNullOrWhiteSpace(name) && config?.Value<string>("addr") is { } deviceAddr)
            {
                name = $"BLU {deviceAddr}";
            }
            else if (type.Type == "bthomesensor")
            {
                var kind = BtHomeObjectName(config?.Value<int?>("obj_id"));
                var addr = config?.Value<string>("addr");
                var deviceName = addr != null && bthomeNames.TryGetValue(addr.ToLowerInvariant(), out var n) ? n : null;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = string.Join(" ", new[] { deviceName ?? (addr != null ? $"BLU {addr}" : null), kind ?? $"Sensor {id}" }.Where(a => a != null));
                }
                else if (deviceName != null && !name.Contains(deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    name = $"{deviceName} {name}";
                }
            }

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private static IReadOnlyDictionary<string, string> BtHomeDeviceNames(JObject? config)
        {
            var names = new Dictionary<string, string>();
            if (config == null)
            {
                return names;
            }

            foreach (var property in config.Properties().Where(a => a.Name.StartsWith("bthomedevice:", StringComparison.Ordinal)))
            {
                var addr = property.Value.Value<string>("addr");
                var name = property.Value.Value<string>("name");
                // only given names: a sensor with an own name is not prefixed with "BLU <addr>"
                if (addr != null && !string.IsNullOrWhiteSpace(name))
                {
                    names[addr.ToLowerInvariant()] = name;
                }
            }
            return names;
        }

        /// <summary>
        /// BTHome v2 object ids (https://bthome.io/format/) of the values BLU devices send.
        /// </summary>
        internal static string? BtHomeObjectName(int? objectId)
        {
            return objectId switch
            {
                0x01 => "Battery",
                0x02 or 0x45 => "Temperature",
                0x03 or 0x2E => "Humidity",
                0x04 => "Pressure",
                0x05 => "Illuminance",
                0x08 => "Dew point",
                0x0C => "Voltage",
                0x12 => "CO2",
                0x14 or 0x2F => "Moisture",
                0x1A => "Door",
                0x1E => "Light",
                0x20 => "Moisture alarm",
                0x21 => "Motion",
                0x22 => "Moving",
                0x23 => "Occupancy",
                0x2D => "Window",
                0x3A => "Button",
                0x3F => "Rotation",
                0x40 or 0x41 => "Distance",
                0x2C => "Vibration",
                0x2B => "Tamper",
                _ => objectId.HasValue ? $"Object 0x{objectId.Value.ToString("x2", CultureInfo.InvariantCulture)}" : null
            };
        }

        private static string MacFromId(string? id) => id == null ? "" : id.Split('-')[^1];

        private static string Suffix(string mac) => mac.Length >= 6 ? mac.Substring(mac.Length - 6) : mac;

        private static string FirstNonEmpty(params string?[] values) => values.First(a => !string.IsNullOrWhiteSpace(a))!;
    }
}
