#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Media;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Media
{
    /// <summary>
    /// The radio station catalog of the core: the built-in stations plus the stations the administrator added
    /// </summary>
    public interface IRadioStationService : IRadioStationCatalog
    {
        /// <summary>
        /// The stations the administrator added (or changed: a custom station with the key of a built-in one replaces it)
        /// </summary>
        IReadOnlyList<RadioStation> CustomStations { get; }

        /// <summary>
        /// Replaces the custom stations. Throws <see cref="ArgumentException"/> for an invalid station.
        /// </summary>
        Task<IReadOnlyList<RadioStation>> SaveCustomStationsAsync(IEnumerable<RadioStation> stations, CancellationToken token = default);
    }

    /// <summary>
    /// The custom stations live as json in the hidden setting <see cref="SettingKey"/>. They are read on first use (not in the
    /// constructor: the singleton is resolved before the tables exist in some tests); when the database cannot be read the
    /// built-in stations are used and the next access tries again.
    /// </summary>
    public sealed class RadioStationService : IRadioStationService
    {
        public const string SettingKey = "RadioStations";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IDbContextFactory<AutomaticaContext> _contextFactory;
        private readonly ILogger<RadioStationService> _logger;
        private readonly RadioStationCatalog _builtIn;
        private readonly object _lock = new();
        private readonly SemaphoreSlim _saveGate = new(1, 1);

        private volatile State? _state;

        public RadioStationService(IDbContextFactory<AutomaticaContext> contextFactory, ILogger<RadioStationService> logger)
            : this(contextFactory, logger, RadioStationCatalog.BuiltIn)
        {
        }

        internal RadioStationService(IDbContextFactory<AutomaticaContext> contextFactory, ILogger<RadioStationService> logger, RadioStationCatalog builtIn)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _builtIn = builtIn;
        }

        public IReadOnlyList<RadioStation> Stations => Current.Catalog.Stations;

        public IReadOnlyList<RadioStation> CustomStations => Current.Custom;

        public RadioStation? Find(string? keyOrName) => Current.Catalog.Find(keyOrName);

        private State Current
        {
            get
            {
                var state = _state;
                if (state != null)
                {
                    return state;
                }

                lock (_lock)
                {
                    if (_state != null)
                    {
                        return _state;
                    }

                    try
                    {
                        using var context = _contextFactory.CreateDbContext();
                        var setting = context.Settings.AsNoTracking().FirstOrDefault(a => a.ValueKey == SettingKey);
                        _state = CreateState(ParseCustom(setting?.ValueText, _logger));
                        return _state;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Radio stations: could not read the custom stations, using the built-in stations");
                        return CreateState(Array.Empty<RadioStation>());
                    }
                }
            }
        }

        public async Task<IReadOnlyList<RadioStation>> SaveCustomStationsAsync(IEnumerable<RadioStation> stations, CancellationToken token = default)
        {
            var custom = Validate(stations);

            await _saveGate.WaitAsync(token);
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(token);
                var setting = await context.Settings.FirstOrDefaultAsync(a => a.ValueKey == SettingKey, token);
                if (setting == null)
                {
                    setting = new Setting
                    {
                        ValueKey = SettingKey,
                        Type = (long)PropertyTemplateType.Text,
                        Group = "SERVER.MEDIA",
                        IsVisible = false,
                        IsReadonly = true,
                        Order = 1000,
                        ReloadContext = SettingReloadContext.None,
                        CreatedAt = DateTimeOffset.Now
                    };
                    context.Settings.Add(setting);
                }

                setting.ValueText = JsonSerializer.Serialize(custom, JsonOptions);
                setting.ModifiedAt = DateTimeOffset.Now;
                await context.SaveChangesAsync(token);

                lock (_lock)
                {
                    _state = CreateState(custom);
                }

                _logger.LogInformation("Radio stations: saved {Count} custom stations", custom.Count);
                return custom;
            }
            finally
            {
                _saveGate.Release();
            }
        }

        /// <summary>
        /// Checks and normalizes the custom stations: key, name and stream url are required, a key only once
        /// </summary>
        internal static IReadOnlyList<RadioStation> Validate(IEnumerable<RadioStation>? stations)
        {
            var result = new List<RadioStation>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var station in stations ?? Array.Empty<RadioStation>())
            {
                if (station == null)
                {
                    continue;
                }

                var key = station.Key?.Trim().ToLowerInvariant();
                var name = station.Name?.Trim();
                var streamUrl = station.StreamUrl?.Trim();
                var logo = string.IsNullOrWhiteSpace(station.Logo) ? null : station.Logo.Trim();
                var tuneIn = string.IsNullOrWhiteSpace(station.TuneInId) ? null : station.TuneInId.Trim();

                if (!RadioStationCatalog.IsValidKey(key))
                {
                    throw new ArgumentException($"Invalid station key \"{station.Key}\" (lower case letters, digits and '-')");
                }
                if (!keys.Add(key!))
                {
                    throw new ArgumentException($"The station key \"{key}\" is used twice");
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"The station \"{key}\" has no name");
                }
                if (!RadioStationCatalog.IsValidStreamUrl(streamUrl))
                {
                    throw new ArgumentException($"The station \"{key}\" has no valid http(s) stream url");
                }
                if (logo != null && !RadioStationCatalog.IsValidStreamUrl(logo))
                {
                    throw new ArgumentException($"The logo of the station \"{key}\" is not a valid http(s) url");
                }
                if (tuneIn != null)
                {
                    tuneIn = tuneIn.All(char.IsDigit) ? "s" + tuneIn : tuneIn.ToLowerInvariant();
                    if (tuneIn.Length < 2 || tuneIn[0] != 's' || !tuneIn.Skip(1).All(char.IsDigit))
                    {
                        throw new ArgumentException($"The TuneIn id of the station \"{key}\" must look like s8007");
                    }
                }

                result.Add(new RadioStation
                {
                    Key = key!,
                    Name = name!,
                    Country = string.IsNullOrWhiteSpace(station.Country) ? null : station.Country.Trim().ToUpperInvariant(),
                    Genre = string.IsNullOrWhiteSpace(station.Genre) ? null : station.Genre.Trim(),
                    StreamUrl = streamUrl!,
                    Logo = logo,
                    Codec = string.IsNullOrWhiteSpace(station.Codec) ? null : station.Codec.Trim(),
                    TuneInId = tuneIn,
                    IsCustom = true
                });
            }

            return result;
        }

        internal static IReadOnlyList<RadioStation> ParseCustom(string? json, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<RadioStation>();
            }

            try
            {
                var stations = JsonSerializer.Deserialize<List<RadioStation>>(json, JsonOptions) ?? new List<RadioStation>();
                return stations
                    .Where(a => a != null && RadioStationCatalog.IsValidKey(a.Key) && RadioStationCatalog.IsValidStreamUrl(a.StreamUrl))
                    .Select(a => a with { IsCustom = true })
                    .ToList();
            }
            catch (JsonException e)
            {
                logger?.LogWarning(e, "Radio stations: the custom stations could not be read");
                return Array.Empty<RadioStation>();
            }
        }

        private State CreateState(IReadOnlyList<RadioStation> custom)
        {
            return new State(new RadioStationCatalog(_builtIn.Stations.Concat(custom)), custom);
        }

        private sealed record State(RadioStationCatalog Catalog, IReadOnlyList<RadioStation> Custom);
    }
}
