using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.HomeConnect.Driver
{
    /// <summary>
    /// A home appliance (haId) of the account
    /// </summary>
    internal sealed class HomeConnectAppliance : DriverNoneAttributeBase
    {
        private static readonly HashSet<string> RunningStates = new() { "Run", "DelayedStart", "Pause", "ActionRequired" };

        private readonly HomeConnectDriver _account;
        private readonly List<ValueNode> _values = new();
        private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;

        public HomeConnectAppliance(IDriverContext driverContext, HomeConnectDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string HaId { get; private set; }

        internal bool Connected { get; private set; }

        internal string OperationState { get; private set; }

        internal IReadOnlyList<ValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            HaId = GetPropertyValueString(HomeConnectDriverFactory.HaIdKey)?.Trim();
            if (string.IsNullOrWhiteSpace(HaId))
            {
                DriverContext.Logger.LogError($"{FullName}: no appliance id (haId) configured, use the scan of the account");
            }
            return base.Init(token);
        }

        private void Set(string key, object value)
        {
            if (value == null)
            {
                return;
            }
            foreach (var node in _values.Where(v => v.Key == key))
            {
                node.Set(value);
            }
        }

        internal void SetConnected(bool connected)
        {
            Connected = connected;
            Set(HomeConnectDataPoints.ConnectedKey, connected);
        }

        /// <summary>
        /// Reads status, settings and programs. Skipped while the appliance is offline.
        /// </summary>
        internal async Task RefreshAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(HaId) || !Connected)
            {
                return;
            }
            _lastRefresh = DriverContext.TimeProvider.GetUtcNow();
            foreach (var item in await _account.Api.GetStatusAsync(HaId, token))
            {
                Apply(item.Key, item.Value);
            }
            foreach (var item in await _account.Api.GetSettingsAsync(HaId, token))
            {
                Apply(item.Key, item.Value);
            }

            var active = await _account.Api.GetActiveProgramAsync(HaId, token);
            ApplyProgram(HomeConnectDataPoints.ActiveProgram, active);
            if (_values.Any(v => v.Key == "selected_program"))
            {
                ApplyProgram(HomeConnectDataPoints.SelectedProgram, await _account.Api.GetSelectedProgramAsync(HaId, token));
            }
        }

        /// <summary>
        /// Refreshes after a reconnect, at most once a minute
        /// </summary>
        internal async Task RefreshThrottledAsync(CancellationToken token = default)
        {
            if (DriverContext.TimeProvider.GetUtcNow() - _lastRefresh < TimeSpan.FromMinutes(1))
            {
                return;
            }
            await RefreshAsync(token);
        }

        private void ApplyProgram(string root, HomeConnectProgram program)
        {
            if (program == null)
            {
                Apply(root, default);
                return;
            }
            Apply(root, JsonSerializer.SerializeToElement(program.Key));
            if (root == HomeConnectDataPoints.ActiveProgram)
            {
                foreach (var option in program.Options)
                {
                    Apply(option.Key, option.Value);
                }
            }
        }

        /// <summary>
        /// Applies one Home Connect value (status, setting, option, program, event)
        /// </summary>
        internal void Apply(string key, JsonElement value, bool isEvent = false)
        {
            var scalar = Conv.Scalar(value);
            switch (key)
            {
                case HomeConnectDataPoints.OperationState:
                    OperationState = HomeConnectApi.ShortEnum(scalar as string);
                    Set("operation_state", OperationState);
                    if (OperationState != null)
                    {
                        Set(HomeConnectDataPoints.RunningKey, RunningStates.Contains(OperationState));
                        Set(HomeConnectDataPoints.PausedKey, OperationState == "Pause");
                    }
                    break;
                case HomeConnectDataPoints.DoorState:
                    Set("door_state", HomeConnectApi.ShortEnum(scalar as string));
                    break;
                case HomeConnectDataPoints.RemoteControlActive:
                    Set("remote_control_active", scalar as bool?);
                    break;
                case HomeConnectDataPoints.RemoteControlStartAllowed:
                    Set("remote_start_allowed", scalar as bool?);
                    break;
                case HomeConnectDataPoints.LocalControlActive:
                    Set("local_control_active", scalar as bool?);
                    break;
                case HomeConnectDataPoints.PowerState:
                    if (scalar is string power)
                    {
                        Set("power", power == HomeConnectDataPoints.PowerOn);
                    }
                    break;
                case HomeConnectDataPoints.ActiveProgram:
                    Set("active_program", scalar as string ?? "");
                    if (scalar == null)
                    {
                        Set("remaining_time", 0);
                        Set("progress", 0);
                        Set("elapsed_time", 0);
                    }
                    break;
                case HomeConnectDataPoints.SelectedProgram:
                    Set("selected_program", scalar as string ?? "");
                    break;
                case HomeConnectDataPoints.RemainingProgramTime:
                    Set("remaining_time", ToInt(scalar));
                    break;
                case HomeConnectDataPoints.ElapsedProgramTime:
                    Set("elapsed_time", ToInt(scalar));
                    break;
                case HomeConnectDataPoints.ProgramProgress:
                    Set("progress", ToInt(scalar));
                    break;
            }

            if (isEvent && (scalar is not string present || !present.Contains(".EventPresentState.", StringComparison.Ordinal) || present.EndsWith(".Present", StringComparison.Ordinal)))
            {
                Set(HomeConnectDataPoints.LastEventKey, key);
            }

            foreach (var custom in _values.Where(v => HomeConnectDataPoints.IsCustom(v.Key) && v.Parameter == key))
            {
                var converted = ConvertCustom(custom.Key, scalar);
                if (converted != null)
                {
                    custom.Set(converted);
                }
            }
        }

        private static object ToInt(object value) => value is int or double ? (int)Math.Round(Conv.ToDouble(value)) : null;

        private static object ConvertCustom(string customKey, object value)
        {
            if (value == null)
            {
                return null;
            }
            return customKey switch
            {
                HomeConnectDataPoints.CustomNumberKey => value is int or double ? Conv.ToDouble(value) : null,
                HomeConnectDataPoints.CustomSwitchKey => value is bool b ? b : value is string s ? s.EndsWith(".On", StringComparison.Ordinal) : Conv.ToBool(value),
                _ => Conv.ToText(value)
            };
        }

        internal async Task WriteAsync(string key, string parameter, object value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(HaId))
            {
                return;
            }
            var api = _account.Api;
            switch (key)
            {
                case "power":
                    await SetPowerAsync(Conv.ToBool(value), token);
                    break;
                case "active_program":
                {
                    var program = Conv.ToText(value).Trim();
                    if (program.Length == 0)
                    {
                        await api.StopProgramAsync(HaId, token);
                    }
                    else
                    {
                        await api.StartProgramAsync(HaId, program, null, token);
                    }
                    break;
                }
                case "selected_program":
                    await api.SelectProgramAsync(HaId, Conv.ToText(value).Trim(), token);
                    break;
                case HomeConnectDataPoints.RunningKey:
                    if (Conv.ToBool(value))
                    {
                        var selected = await api.GetSelectedProgramAsync(HaId, token);
                        if (selected == null)
                        {
                            throw new HomeConnectApiException("No program selected on the appliance");
                        }
                        // only options the user can set are accepted when starting
                        await api.StartProgramAsync(HaId, selected.Key, selected.Options.Where(o => !o.Key.StartsWith("BSH.Common.Option.", StringComparison.Ordinal) ||
                            o.Key is "BSH.Common.Option.StartInRelative" or "BSH.Common.Option.FinishInRelative").ToList(), token);
                    }
                    else
                    {
                        await api.StopProgramAsync(HaId, token);
                    }
                    break;
                case HomeConnectDataPoints.PausedKey:
                    await api.CommandAsync(HaId, Conv.ToBool(value) ? HomeConnectDataPoints.PauseProgram : HomeConnectDataPoints.ResumeProgram, token);
                    break;
                default:
                    if (HomeConnectDataPoints.IsCustom(key))
                    {
                        await WriteCustomAsync(key, parameter, value, token);
                    }
                    break;
            }
        }

        private async Task SetPowerAsync(bool on, CancellationToken token)
        {
            if (on)
            {
                await _account.Api.SetSettingAsync(HaId, HomeConnectDataPoints.PowerState, HomeConnectDataPoints.PowerOn, token);
                return;
            }
            try
            {
                await _account.Api.SetSettingAsync(HaId, HomeConnectDataPoints.PowerState, HomeConnectDataPoints.PowerOff, token);
            }
            catch (HomeConnectApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Forbidden)
            {
                // many appliances only know On / Standby
                await _account.Api.SetSettingAsync(HaId, HomeConnectDataPoints.PowerState, HomeConnectDataPoints.PowerStandby, token);
            }
        }

        private async Task WriteCustomAsync(string customKey, string hcKey, object value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(hcKey))
            {
                return;
            }
            object payload = customKey switch
            {
                HomeConnectDataPoints.CustomNumberKey => Conv.ToDouble(value) is var d && d == Math.Round(d) ? (object)(long)d : d,
                HomeConnectDataPoints.CustomSwitchKey => Conv.ToBool(value),
                _ => Conv.ToText(value)
            };

            if (hcKey.Contains(".Command.", StringComparison.Ordinal))
            {
                if (Conv.ToBool(value))
                {
                    await _account.Api.CommandAsync(HaId, hcKey, token);
                }
            }
            else if (hcKey.Contains(".Option.", StringComparison.Ordinal))
            {
                await _account.Api.SetActiveOptionAsync(HaId, hcKey, payload, token);
            }
            else if (hcKey.Contains(".Setting.", StringComparison.Ordinal))
            {
                await _account.Api.SetSettingAsync(HaId, hcKey, payload, token);
            }
            else
            {
                DriverContext.Logger.LogWarning($"{FullName}: {hcKey} is read-only");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await RefreshAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = HomeConnectDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            ValueNode node = null;
            node = new ValueNode(ctx, key, dataPoint.IsWriteable ? (v, t) => WriteAsync(key, node!.Parameter, v, t) : null, t => Read(t))
            {
                Parameter = HomeConnectDataPoints.IsCustom(key) ? ctx.NodeInstance.GetPropertyValueString(HomeConnectDriverFactory.CustomKeyKey)?.Trim() : null
            };
            _values.Add(node);
            return node;
        }
    }
}
