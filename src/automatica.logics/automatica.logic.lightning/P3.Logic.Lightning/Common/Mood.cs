using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace P3.Logic.Lightning.Common
{
    /// <summary>
    /// A lighting mood (scene): a name and one value per circuit in the text form of <see cref="LightValue.Parse"/>
    /// </summary>
    public sealed class Mood
    {
        /// <summary>Loxone numbering: 99 = all on, 0 = all off, user moods 1..89 (98 is reserved for the alarm clock)</summary>
        public const int AllOn = 99;
        public const int AllOff = 0;
        /// <summary>circuits were changed one by one, no mood is active</summary>
        public const int Manual = -1;
        public const int MaxUserMood = 89;

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// circuit index (0 based) → value text; a missing circuit is off
        /// </summary>
        [JsonProperty("values")]
        public Dictionary<int, string> Values { get; set; } = new();

        public static bool IsBuiltIn(int id) => id == AllOn || id == AllOff || id == 98;

        public static bool IsUserMood(int id) => id >= 1 && id <= MaxUserMood;

        /// <summary>
        /// Parses the mood parameter: "Reading:1=100,2=on;Cosy:1=30,3=#FF8800,4=40@2700" (circuits from 1).
        /// The moods get the ids 1, 2, … in the given order; broken entries are skipped.
        /// </summary>
        public static List<Mood> ParseDefinition(string text, int circuits)
        {
            var moods = new List<Mood>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return moods;
            }

            foreach (var entry in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var colon = entry.IndexOf(':');
                var name = colon >= 0 ? entry[..colon].Trim() : entry.Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                var mood = new Mood { Id = moods.Count + 1, Name = name };
                if (colon >= 0)
                {
                    foreach (var part in entry[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var eq = part.IndexOf('=');
                        if (eq <= 0 || !int.TryParse(part[..eq].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var circuit))
                        {
                            continue;
                        }
                        if (circuit < 1 || circuit > circuits)
                        {
                            continue;
                        }
                        mood.Values[circuit - 1] = part[(eq + 1)..].Trim();
                    }
                }
                moods.Add(mood);
            }
            return moods;
        }

        public static string Serialize(IEnumerable<Mood> moods) => JsonConvert.SerializeObject(moods.ToList());

        public static List<Mood> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<List<Mood>>(json)?.Where(a => a != null && IsUserMood(a.Id)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
