#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace Automatica.Driver.ShellyFactory.Catalog
{
    internal enum ShellyApi
    {
        /// <summary>Gen2, Gen3, Gen4: JSON-RPC, components "type:id"</summary>
        Rpc,
        /// <summary>Gen1: REST /status sections</summary>
        Gen1
    }

    internal enum ShellyComponentLayout
    {
        /// <summary>Gen2+ "switch:0"</summary>
        Keyed,
        /// <summary>Gen2+ "sys", "wifi" (no id)</summary>
        Single,
        /// <summary>Gen1 "relays": [ ... ]</summary>
        Array,
        /// <summary>Gen1 "ext_temperature": { "0": {...} }</summary>
        Map,
        /// <summary>Gen1 values directly in the status root (tmp, hum, bat...)</summary>
        Root
    }

    /// <summary>
    /// What a Gen2+ write sends: the RPC method and its parameters.
    /// </summary>
    internal delegate (string Method, JObject Params)? RpcWrite(object value, int id, JObject? componentStatus);

    /// <summary>
    /// The context of a Gen1 write: device type (SHRGBW2...), mode from /settings (color/white/relay/roller) and the cached component status.
    /// </summary>
    internal sealed record Gen1WriteContext(string? DeviceType, string? Mode, JToken? ComponentStatus);

    /// <summary>
    /// What a Gen1 write requests: the URL path and query, e.g. "/relay/0?turn=on".
    /// </summary>
    internal delegate string? Gen1Write(object value, int index, Gen1WriteContext context);

    /// <summary>
    /// A data point of a component: a value at <see cref="Path"/> inside the component status, optionally writeable.
    /// </summary>
    internal sealed class ShellyDataPoint
    {
        public ShellyDataPoint(string key, string i18n, NodeDataType type, string? path)
        {
            Key = key;
            I18N = i18n;
            Type = type;
            Path = path;
        }

        public string Key { get; }
        public string I18N { get; }
        public NodeDataType Type { get; }

        /// <summary>JSON path inside the component status, null for commands and events</summary>
        public string? Path { get; }

        public Func<JToken, object?>? ReadFunc { get; private set; }
        public RpcWrite? RpcWriter { get; private set; }
        public Gen1Write? Gen1Writer { get; private set; }

        /// <summary>Filled from NotifyEvent (Gen2+) / inputs[].event (Gen1)</summary>
        public bool IsEvent { get; private set; }

        /// <summary>Write only (open, close, stop, mute...)</summary>
        public bool IsCommand { get; private set; }

        /// <summary>Gen1 CoIoT sensor descriptions ("output", "power"...) that carry this value</summary>
        public string[] CoIoT { get; private set; } = Array.Empty<string>();

        /// <summary>Whether the scan creates this data point for a component status, default: the path exists</summary>
        public Func<JToken, JToken?, bool>? IncludeFunc { get; private set; }

        public Guid Template { get; set; }

        public bool IsWriteable => RpcWriter != null || Gen1Writer != null;
        public bool IsReadable => !IsCommand;

        public ShellyDataPoint Read(Func<JToken, object?> read)
        {
            ReadFunc = read;
            return this;
        }

        public ShellyDataPoint Rpc(RpcWrite write)
        {
            RpcWriter = write;
            return this;
        }

        public ShellyDataPoint Gen1(Gen1Write write)
        {
            Gen1Writer = write;
            return this;
        }

        public ShellyDataPoint Command()
        {
            IsCommand = true;
            return this;
        }

        public ShellyDataPoint Event()
        {
            IsEvent = true;
            return this;
        }

        public ShellyDataPoint Co(params string[] descriptions)
        {
            CoIoT = descriptions;
            return this;
        }

        /// <param name="include">(component status, complete device status) → create the data point?</param>
        public ShellyDataPoint When(Func<JToken, JToken?, bool> include)
        {
            IncludeFunc = include;
            return this;
        }

        public bool ShouldInclude(JToken? componentStatus, JToken? deviceStatus)
        {
            if (IncludeFunc != null)
            {
                return componentStatus != null && IncludeFunc(componentStatus, deviceStatus);
            }

            if (IsCommand || IsEvent)
            {
                return true;
            }

            return Select(componentStatus) != null;
        }

        public JToken? Select(JToken? componentStatus)
        {
            if (componentStatus == null || Path == null)
            {
                return null;
            }

            var token = componentStatus.SelectToken(Path);
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        public object? Convert(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            try
            {
                return ReadFunc != null ? ReadFunc(token) : ShellyValues.ByType(token, Type);
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or ArgumentException or OverflowException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A component type, e.g. Gen2+ "switch" or Gen1 "relays", with its data points.
    /// </summary>
    internal sealed class ShellyComponentType
    {
        public ShellyComponentType(ShellyApi api, string type, string i18n, Guid template, ShellyComponentLayout layout, IEnumerable<ShellyDataPoint> dataPoints)
        {
            Api = api;
            Type = type;
            I18N = i18n;
            Template = template;
            Layout = layout;
            Interface = ShellyGuid.Create(template, "interface");
            DataPoints = dataPoints.ToList();

            foreach (var dp in DataPoints)
            {
                dp.Template = ShellyGuid.Create(template, dp.Key);
            }

            if (DataPoints.Select(a => a.Key).Distinct().Count() != DataPoints.Count)
            {
                throw new ArgumentException($"Duplicate data point key in {type}");
            }
        }

        public ShellyApi Api { get; }

        /// <summary>Gen2+ component type ("switch") or Gen1 status section ("relays", "" for root values)</summary>
        public string Type { get; }
        public string I18N { get; }
        public Guid Template { get; }
        public Guid Interface { get; }
        public ShellyComponentLayout Layout { get; }
        public IReadOnlyList<ShellyDataPoint> DataPoints { get; }

        /// <summary>Gen1 CoIoT block names ("relay", "light"...) the values of this component come in, empty: any block</summary>
        public string[] CoIoTBlocks { get; init; } = Array.Empty<string>();

        public bool HasId => Layout is ShellyComponentLayout.Keyed or ShellyComponentLayout.Array or ShellyComponentLayout.Map;

        /// <summary>Gen2+ status key, e.g. "switch:0"</summary>
        public string Key(int id) => HasId ? $"{Type}:{id}" : Type;

        public ShellyDataPoint? GetDataPoint(string key) => DataPoints.FirstOrDefault(a => a.Key == key);

        /// <summary>
        /// The status of instance <paramref name="id"/> inside a complete (or partial) device status.
        /// </summary>
        public JToken? Select(JToken? status, int id)
        {
            if (status is not JObject root)
            {
                return null;
            }

            switch (Layout)
            {
                case ShellyComponentLayout.Keyed:
                case ShellyComponentLayout.Single:
                    return root[Key(id)] is JObject o ? o : null;
                case ShellyComponentLayout.Array:
                    return root[Type] is JArray a && id >= 0 && id < a.Count ? a[id] : null;
                case ShellyComponentLayout.Map:
                    return root[Type] is JObject m ? m[id.ToString(CultureInfo.InvariantCulture)] : null;
                case ShellyComponentLayout.Root:
                    return root;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The ids of all instances of this component in a complete device status.
        /// </summary>
        public IEnumerable<int> InstanceIds(JObject status)
        {
            switch (Layout)
            {
                case ShellyComponentLayout.Keyed:
                    foreach (var property in status.Properties())
                    {
                        var colon = property.Name.IndexOf(':');
                        if (colon > 0 && property.Name.Substring(0, colon) == Type &&
                            int.TryParse(property.Name.AsSpan(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                        {
                            yield return id;
                        }
                    }
                    break;
                case ShellyComponentLayout.Single:
                    if (status[Type] is JObject)
                    {
                        yield return 0;
                    }
                    break;
                case ShellyComponentLayout.Array:
                    if (status[Type] is JArray array)
                    {
                        for (var i = 0; i < array.Count; i++)
                        {
                            yield return i;
                        }
                    }
                    break;
                case ShellyComponentLayout.Map:
                    if (status[Type] is JObject map)
                    {
                        foreach (var property in map.Properties())
                        {
                            if (int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                            {
                                yield return id;
                            }
                        }
                    }
                    break;
                case ShellyComponentLayout.Root:
                    yield return 0;
                    break;
            }
        }
    }

    internal static class ShellyGuid
    {
        /// <summary>
        /// A stable name based (v5 style) guid: the same template id for the same component and data point key on every start.
        /// </summary>
        public static Guid Create(Guid ns, string name)
        {
            var bytes = SHA1.HashData(ns.ToByteArray().Concat(Encoding.UTF8.GetBytes(name)).ToArray());
            var guid = new byte[16];
            Array.Copy(bytes, guid, 16);
            guid[7] = (byte)((guid[7] & 0x0F) | 0x50);
            guid[8] = (byte)((guid[8] & 0x3F) | 0x80);
            return new Guid(guid);
        }
    }

    internal static class ShellyValues
    {
        public static object? ByType(JToken token, NodeDataType type)
        {
            switch (type)
            {
                case NodeDataType.Boolean:
                    return ToBool(token);
                case NodeDataType.Integer:
                    return (int)Math.Round(ToDouble(token));
                case NodeDataType.Double:
                    return ToDouble(token);
                case NodeDataType.String:
                    return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Newtonsoft.Json.Formatting.None);
                default:
                    return token.ToObject<object>();
            }
        }

        public static bool ToBool(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Integer:
                case JTokenType.Float:
                    return Math.Abs(token.Value<double>()) > double.Epsilon;
                case JTokenType.String:
                    return ToBool(token.Value<string>() ?? "");
                default:
                    throw new FormatException($"{token} is no boolean");
            }
        }

        public static bool ToBool(object? value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    s = s.Trim().ToLowerInvariant();
                    return s is "1" or "true" or "on" or "open" or "opened" or "yes" or "alarm" or "mild" or "heavy" or "detected";
                case JToken t:
                    return ToBool(t);
                default:
                    return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0;
            }
        }

        public static double ToDouble(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Boolean => token.Value<bool>() ? 1 : 0,
                JTokenType.String => double.Parse(token.Value<string>()!, CultureInfo.InvariantCulture),
                _ => token.Value<double>()
            };
        }

        public static double ToDouble(object? value)
        {
            return value switch
            {
                null => 0,
                bool b => b ? 1 : 0,
                string s => double.Parse(s, CultureInfo.InvariantCulture),
                JToken t => ToDouble(t),
                _ => System.Convert.ToDouble(value, CultureInfo.InvariantCulture)
            };
        }

        public static int ToInt(object? value) => (int)Math.Round(ToDouble(value));

        public static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
