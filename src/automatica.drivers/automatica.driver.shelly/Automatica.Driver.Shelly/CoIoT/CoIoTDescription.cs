#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.CoIoT
{
    /// <summary>
    /// A sensor of a CoIoT description: <see cref="Description"/> is its name ("temperature", "output", "power"...),
    /// <see cref="Block"/> the block it belongs to ("relay_0", "sensor_0", "device"...).
    /// </summary>
    public sealed record CoIoTSensor(int Id, string Type, string Description, string? Unit, string Block)
    {
        private static readonly Regex BlockIndex = new(@"^(?<name>[a-zA-Z]+?)_?(?<index>\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// "relay_1" → ("relay", 1), "device" → ("device", 0).
        /// </summary>
        public (string Name, int Index) BlockNameAndIndex
        {
            get
            {
                var match = BlockIndex.Match(Block);
                return match.Success ? (match.Groups["name"].Value.ToLowerInvariant(), int.Parse(match.Groups["index"].Value)) : (Block.ToLowerInvariant(), 0);
            }
        }
    }

    /// <summary>
    /// The CoIoT description of a device (GET /cit/d): which sensor id means what. Needed to read the values of a status packet.
    /// </summary>
    public sealed class CoIoTDescription
    {
        private readonly Dictionary<int, CoIoTSensor> _sensors = new();

        public string Json { get; }

        private CoIoTDescription(string json)
        {
            Json = json;
        }

        public IEnumerable<CoIoTSensor> Sensors => _sensors.Values;

        public CoIoTSensor? Get(int sensorId) => _sensors.TryGetValue(sensorId, out var s) ? s : null;

        public static CoIoTDescription? TryParse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return Parse(JObject.Parse(json));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static CoIoTDescription? Parse(JObject json)
        {
            if (json["sen"] is not JArray sensors)
            {
                return null;
            }

            var blocks = new Dictionary<int, string>();
            if (json["blk"] is JArray blk)
            {
                foreach (var b in blk)
                {
                    var id = b.Value<int?>("I");
                    var name = b.Value<string>("D");
                    if (id.HasValue && name != null)
                    {
                        blocks[id.Value] = name;
                    }
                }
            }

            var description = new CoIoTDescription(json.ToString(Formatting.None));
            foreach (var s in sensors)
            {
                var id = s.Value<int?>("I");
                var name = s.Value<string>("D");
                if (!id.HasValue || name == null)
                {
                    continue;
                }

                // "L" is the block id, an array when the sensor belongs to several blocks: the first one is used
                var blockToken = s["L"];
                var blockId = blockToken?.Type == JTokenType.Array ? blockToken.First?.Value<int?>() : blockToken?.Value<int?>();
                var block = blockId.HasValue && blocks.TryGetValue(blockId.Value, out var blockName) ? blockName : "device";

                description._sensors[id.Value] = new CoIoTSensor(id.Value, s.Value<string>("T") ?? "", name, s.Value<string>("U"), block);
            }

            return description;
        }
    }
}
