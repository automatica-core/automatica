#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Automatica.Core.Base.Media
{
    /// <summary>
    /// An immutable station catalog. A station added later replaces an earlier one with the same key (a custom station can
    /// replace a built-in one).
    /// </summary>
    public sealed class RadioStationCatalog : IRadioStationCatalog
    {
        internal const string BuiltInResource = "Automatica.Core.Base.Media.radio-stations.json";

        private static readonly Lazy<RadioStationCatalog> BuiltInCatalog = new(() => new RadioStationCatalog(LoadBuiltIn()));
        private static readonly Regex KeyPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant);

        internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly Dictionary<string, RadioStation> _byKey;

        /// <summary>
        /// The stations that ship with the core
        /// </summary>
        public static RadioStationCatalog BuiltIn => BuiltInCatalog.Value;

        public IReadOnlyList<RadioStation> Stations { get; }

        public RadioStationCatalog(IEnumerable<RadioStation> stations)
        {
            _byKey = new Dictionary<string, RadioStation>(StringComparer.OrdinalIgnoreCase);
            foreach (var station in stations)
            {
                if (!string.IsNullOrWhiteSpace(station.Key))
                {
                    _byKey[station.Key] = station;
                }
            }

            Stations = _byKey.Values
                .OrderBy(a => a.Country ?? "￿", StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public RadioStation? Find(string? keyOrName)
        {
            var value = keyOrName?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (_byKey.TryGetValue(value, out var station))
            {
                return station;
            }

            return Stations.FirstOrDefault(a => string.Equals(a.Name, value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// A key may be used for a custom station: lower case letters, digits and '-', at most 64 characters
        /// </summary>
        public static bool IsValidKey(string? key)
        {
            return key != null && KeyPattern.IsMatch(key);
        }

        /// <summary>
        /// The stream url of a station must be an absolute http or https url
        /// </summary>
        public static bool IsValidStreamUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);
        }

        /// <summary>
        /// The station keys of a list parameter: "at-oe3;de-1live" (also ',' or one per line), empty entries removed, duplicates once
        /// </summary>
        public static IReadOnlyList<string> ParseKeys(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string JoinKeys(IEnumerable<string> keys)
        {
            return string.Join(";", keys);
        }

        internal static IReadOnlyList<RadioStation> LoadBuiltIn()
        {
            using var stream = typeof(RadioStationCatalog).Assembly.GetManifestResourceStream(BuiltInResource);
            if (stream == null)
            {
                return Array.Empty<RadioStation>();
            }

            return Parse(stream);
        }

        internal static IReadOnlyList<RadioStation> Parse(Stream stream)
        {
            var document = JsonSerializer.Deserialize<BuiltInDocument>(stream, JsonOptions);
            return document?.Stations?.Where(a => IsValidKey(a.Key) && IsValidStreamUrl(a.StreamUrl)).Select(a => a with { IsCustom = false }).ToList()
                   ?? (IReadOnlyList<RadioStation>)Array.Empty<RadioStation>();
        }

        private sealed class BuiltInDocument
        {
            public List<RadioStation>? Stations { get; set; }
        }
    }

    public static class RadioStationCatalogExtensions
    {
        /// <summary>
        /// The station a value stands for: a station of the catalog (key or name), otherwise, as configured before the
        /// catalog, a stream url (anything with a scheme) or a TuneIn id ("s8007" or just the number). Null for an empty value.
        /// </summary>
        public static RadioStation? Resolve(this IRadioStationCatalog catalog, string? value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var station = catalog.Find(value);
            if (station != null)
            {
                return station;
            }

            if (value.Contains(':'))
            {
                return new RadioStation { Key = value, Name = value, StreamUrl = value };
            }

            var tuneIn = value.All(char.IsDigit) ? "s" + value : value;
            if (tuneIn.Length > 1 && (tuneIn[0] == 's' || tuneIn[0] == 'S') && tuneIn.Skip(1).All(char.IsDigit))
            {
                return new RadioStation { Key = value, Name = value, TuneInId = "s" + tuneIn[1..] };
            }

            return null;
        }
    }
}
