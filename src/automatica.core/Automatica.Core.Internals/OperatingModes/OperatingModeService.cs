using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.OperatingModes
{
    /// <summary>
    /// System wide operating modes (public holiday, vacation, school holidays, custom modes)
    /// </summary>
    public interface IOperatingModeService
    {
        OperatingModeConfiguration Configuration { get; }

        /// <summary>Active mode keys of today (local date), highest priority first</summary>
        IReadOnlyList<string> ActiveModes { get; }

        bool IsActive(string mode);

        bool ModeExists(string mode);

        DateOnly Today { get; }

        IReadOnlyList<OperatingModeDay> Days(DateOnly from, int days);

        Task StartAsync(CancellationToken token = default);

        Task StopAsync();

        Task<OperatingModeConfiguration> SaveAsync(OperatingModeConfiguration configuration, CancellationToken token = default);

        /// <summary>
        /// Manual on (true), off (false) or back to the operating times (null). False when the mode does not exist.
        /// </summary>
        Task<bool> SetManualAsync(string mode, bool? state, CancellationToken token = default);
    }

    /// <summary>
    /// Keeps the configuration (hidden setting <see cref="OperatingModeConfiguration.SettingKey"/>), publishes the state for the logics
    /// on the dispatcher (<see cref="OperatingModeContract"/>) and takes their manual on/off requests.
    /// The constructor does not touch the database (the singleton is resolved before the tables exist in some tests).
    /// </summary>
    public sealed class OperatingModeService : IOperatingModeService, IDisposable
    {
        private readonly IDbContextFactory<AutomaticaContext> _contextFactory;
        private readonly IDispatcher _dispatcher;
        private readonly ISettingsCache _settingsCache;
        private readonly ILogger<OperatingModeService> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Action<IDispatchable, DispatchValue> _requestCallback;
        private readonly StateDispatchable _stateDispatchable = new();

        private volatile OperatingModeConfiguration _configuration = OperatingModeConfiguration.CreateDefault();
        private Timer _timer;
        private string _lastState;
        private bool _started;

        public OperatingModeService(IDbContextFactory<AutomaticaContext> contextFactory, IDispatcher dispatcher, ISettingsCache settingsCache, ILogger<OperatingModeService> logger)
        {
            _contextFactory = contextFactory;
            _dispatcher = dispatcher;
            _settingsCache = settingsCache;
            _logger = logger;
            _requestCallback = (source, value) => _ = HandleRequestAsync(value?.Value);
        }

        internal TimeProvider Time { get; set; } = DateTimeHelper.ProviderInstance;

        public OperatingModeConfiguration Configuration => _configuration;

        public DateOnly Today => DateOnly.FromDateTime(Time.GetLocalNow().DateTime);

        public IReadOnlyList<string> ActiveModes => OperatingModeCalculator.For(_configuration, Today).Modes;

        public bool IsActive(string mode) => ActiveModes.Contains(mode ?? "", StringComparer.OrdinalIgnoreCase);

        public bool ModeExists(string mode) => _configuration.Modes.Any(a => string.Equals(a.Key, mode, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<OperatingModeDay> Days(DateOnly from, int days) => OperatingModeCalculator.Range(_configuration, from, Math.Clamp(days, 0, 400));

        public async Task StartAsync(CancellationToken token = default)
        {
            try
            {
                _configuration = await LoadAsync(token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Operating modes: could not load the configuration, using the defaults");
                _configuration = OperatingModeConfiguration.CreateDefault();
            }

            await _dispatcher.RegisterDispatch(DispatchableType.Control, OperatingModeContract.RequestId, _requestCallback);
            _started = true;
            Publish(force: true);

            // date change at midnight, location changes and a dispatcher that lost its values are picked up once a minute
            _timer?.Dispose();
            _timer = new Timer(_ => Publish(force: false), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _logger.LogInformation($"Operating modes started, active today: {string.Join(",", ActiveModes)}");
        }

        public async Task StopAsync()
        {
            _started = false;
            _timer?.Dispose();
            _timer = null;
            try
            {
                await _dispatcher.UnRegisterDispatch(DispatchableType.Control, OperatingModeContract.RequestId, _requestCallback);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Operating modes: could not unregister the request callback");
            }
        }

        public async Task<OperatingModeConfiguration> SaveAsync(OperatingModeConfiguration configuration, CancellationToken token = default)
        {
            var normalized = OperatingModeConfiguration.Normalize(configuration);
            await _gate.WaitAsync(token);
            try
            {
                await StoreAsync(normalized, token);
                _configuration = normalized;
            }
            finally
            {
                _gate.Release();
            }

            Publish(force: false);
            return normalized;
        }

        public async Task<bool> SetManualAsync(string mode, bool? state, CancellationToken token = default)
        {
            await _gate.WaitAsync(token);
            try
            {
                var copy = OperatingModeConfiguration.Parse(_configuration.ToJson());
                var definition = copy.Modes.FirstOrDefault(a => string.Equals(a.Key, mode, StringComparison.OrdinalIgnoreCase));
                if (definition == null)
                {
                    return false;
                }
                if (definition.Manual == state)
                {
                    return true;
                }

                definition.Manual = state;
                await StoreAsync(copy, token);
                _configuration = copy;
                _logger.LogInformation($"Operating mode {definition.Key} manually {(state.HasValue ? (state.Value ? "on" : "off") : "back to the operating times")}");
            }
            finally
            {
                _gate.Release();
            }

            Publish(force: false);
            return true;
        }

        internal async Task HandleRequestAsync(object value)
        {
            try
            {
                using var json = JsonDocument.Parse(value?.ToString() ?? "");
                var root = json.RootElement;
                var mode = root.TryGetProperty("mode", out var m) ? m.GetString() : null;
                bool? state = root.TryGetProperty("state", out var s) && s.ValueKind is JsonValueKind.True or JsonValueKind.False ? s.GetBoolean() : null;
                if (string.IsNullOrWhiteSpace(mode) || !await SetManualAsync(mode, state))
                {
                    _logger.LogWarning($"Operating modes: request for unknown mode {mode} ignored");
                }
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException)
            {
                _logger.LogWarning($"Operating modes: invalid request ignored ({e.Message})");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Operating modes: request failed");
            }
        }

        /// <summary>
        /// Dispatches the state when it changed (or when the dispatcher lost it)
        /// </summary>
        internal void Publish(bool force)
        {
            if (!_started)
            {
                return;
            }

            try
            {
                var latitude = _settingsCache.GetByKey("Latitude")?.ValueDouble;
                var longitude = _settingsCache.GetByKey("Longitude")?.ValueDouble;
                var state = OperatingModeCalculator.StateJson(_configuration, Today, latitude, longitude);
                var missing = _dispatcher.GetValue(DispatchableType.Control, OperatingModeContract.StateId) == null;

                lock (_stateDispatchable)
                {
                    if (!force && !missing && state == _lastState)
                    {
                        return;
                    }
                    _lastState = state;
                }

                _dispatcher.DispatchValue(_stateDispatchable, state);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Operating modes: could not publish the state");
            }
        }

        private async Task<OperatingModeConfiguration> LoadAsync(CancellationToken token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            var setting = await context.Settings.AsNoTracking().FirstOrDefaultAsync(a => a.ValueKey == OperatingModeConfiguration.SettingKey, token);
            return OperatingModeConfiguration.Parse(setting?.ValueText);
        }

        private async Task StoreAsync(OperatingModeConfiguration configuration, CancellationToken token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            var setting = await context.Settings.FirstOrDefaultAsync(a => a.ValueKey == OperatingModeConfiguration.SettingKey, token);
            if (setting == null)
            {
                setting = new Setting
                {
                    ValueKey = OperatingModeConfiguration.SettingKey,
                    Type = (long)PropertyTemplateType.Text,
                    Group = "SERVER.OPERATING_MODES",
                    IsVisible = false,
                    IsReadonly = true,
                    Order = 1000,
                    ReloadContext = SettingReloadContext.None,
                    CreatedAt = DateTimeOffset.Now
                };
                context.Settings.Add(setting);
            }

            setting.ValueText = configuration.ToJson();
            setting.ModifiedAt = DateTimeOffset.Now;
            await context.SaveChangesAsync(token);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _gate.Dispose();
        }

        private sealed class StateDispatchable : IDispatchable
        {
            public DispatchableSource Source => DispatchableSource.NodeInstance;
            public DispatchableType Type => DispatchableType.Control;
            public string Name => "OperatingModes";
            public Guid Id => OperatingModeContract.StateId;
            public bool IsRemanent => false;
        }
    }
}
