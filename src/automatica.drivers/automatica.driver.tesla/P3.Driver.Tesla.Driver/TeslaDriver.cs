using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Tesla.Driver
{
    /// <summary>
    /// A Tesla account. Polls the vehicles; vehicle data is only requested while a vehicle is online, so polling never keeps a car awake.
    /// </summary>
    internal sealed class TeslaDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<TeslaVehicle> _vehicles = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal string AuthUrl { get; set; } = TeslaApi.DefaultAuthUrl;
        internal TeslaApi Api { get; private set; }
        internal IReadOnlyList<TeslaVehicle> Vehicles => _vehicles;

        /// <summary>
        /// How long a command waits for a sleeping vehicle to come online
        /// </summary>
        internal TimeSpan WakeTimeout { get; set; } = TimeSpan.FromSeconds(45);
        internal TimeSpan WakePollInterval { get; set; } = TimeSpan.FromSeconds(3);

        public TeslaDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(60, GetPropertyValueInt(TeslaDriverFactory.PollKey)));

            var apiUrl = GetPropertyValueString(TeslaDriverFactory.ApiUrlKey);
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                apiUrl = TeslaDriverFactory.DefaultApiUrl;
            }

            var configuredToken = GetPropertyValueString(TeslaDriverFactory.RefreshTokenKey);
            var refreshToken = TokenState.Resolve(GetPropertyValueString(TeslaDriverFactory.TokenStateKey), configuredToken);

            Api = new TeslaApi(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(), apiUrl,
                GetPropertyValueString(TeslaDriverFactory.CommandUrlKey), AuthUrl, GetPropertyValueString(TeslaDriverFactory.ClientIdKey), refreshToken);
            Api.RefreshTokenRotated += rotated => SaveTokenState(TokenState.Serialize(configuredToken, rotated));

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                DriverContext.Logger.LogError("No Tesla refresh token configured");
            }
            return base.Init(token);
        }

        private void SaveTokenState(string json)
        {
            var property = GetProperty(TeslaDriverFactory.TokenStateKey);
            if (property == null)
            {
                return;
            }
            property.Value = json;
            DriverContext.NodeTemplateFactory.SetPropertyValue(property.ObjId, json);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;

            try
            {
                await PollAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError(ex, "First Tesla poll failed, retrying with the next poll");
            }

            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            return base.Stop(token);
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                await PollAsync();
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Tesla poll failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                foreach (var vehicle in _vehicles)
                {
                    try
                    {
                        await vehicle.RefreshAsync(token);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && ex is TeslaApiException or HttpRequestException or JsonException)
                    {
                        DriverContext.Logger.LogError($"Could not read {vehicle.Vin}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != "vehicle")
            {
                return null;
            }
            var vehicle = new TeslaVehicle(ctx, this);
            _vehicles.Add(vehicle);
            return vehicle;
        }
    }

    /// <summary>
    /// Persists the rotated refresh token together with a hash of the configured one: when the user enters a new token
    /// in the settings, the stored (rotated) one is discarded.
    /// </summary>
    internal static class TokenState
    {
        private sealed class State
        {
            public string Source { get; set; }
            public string Token { get; set; }
        }

        public static string Serialize(string configured, string rotated)
        {
            return JsonSerializer.Serialize(new State { Source = Hash(configured), Token = rotated });
        }

        public static string Resolve(string stateJson, string configured)
        {
            if (string.IsNullOrWhiteSpace(stateJson))
            {
                return configured;
            }
            try
            {
                var state = JsonSerializer.Deserialize<State>(stateJson);
                return state != null && state.Source == Hash(configured) && !string.IsNullOrWhiteSpace(state.Token) ? state.Token : configured;
            }
            catch (JsonException)
            {
                return configured;
            }
        }

        private static string Hash(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")));
        }
    }
}
