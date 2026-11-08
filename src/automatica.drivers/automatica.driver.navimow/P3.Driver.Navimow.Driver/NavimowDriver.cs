using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Navimow.Driver
{
    /// <summary>
    /// Persisted login: the tokens plus a hash of the configured authorization code / refresh token, so a newly entered one wins
    /// </summary>
    internal sealed class NavimowTokenState
    {
        public string Source { get; set; }
        public string Refresh { get; set; }
        public string Access { get; set; }
        public DateTimeOffset Expires { get; set; }

        public static string Hash(string code, string refresh) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((code ?? "").Trim() + "\n" + (refresh ?? "").Trim())));

        public static NavimowTokenState Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<NavimowTokenState>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A Navimow account: polls the status of all its mowers with one request
    /// </summary>
    internal sealed class NavimowDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<NavimowMowerNode> _mowers = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);

        private string _source;
        private string _pendingCode;
        private string _redirectUri;
        private string _loginUrl;
        private TimeSpan _poll;
        private Timer _timer;
        private Dictionary<string, NavimowDevice> _devices = new();
        private DateTimeOffset _devicesRead = DateTimeOffset.MinValue;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal TimeSpan DeviceListInterval { get; set; } = TimeSpan.FromHours(1);
        internal TimeSpan CommandRefreshDelay { get; set; } = TimeSpan.FromSeconds(5);

        internal NavimowApi Api { get; private set; }
        internal IReadOnlyList<NavimowMowerNode> Mowers => _mowers;
        internal string Status { get; private set; }

        public NavimowDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        private DateTimeOffset Now() => DriverContext.TimeProvider.GetUtcNow();

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(NavimowDriverFactory.PollKey), 30, 3600));
            var configuredCode = GetPropertyValueString(NavimowDriverFactory.AuthorizationCodeKey)?.Trim() ?? "";
            var configuredRefresh = GetPropertyValueString(NavimowDriverFactory.RefreshTokenKey)?.Trim() ?? "";
            _redirectUri = GetPropertyValueString(NavimowDriverFactory.RedirectUriKey)?.Trim();
            _loginUrl = GetPropertyValueString(NavimowDriverFactory.LoginUrlKey)?.Trim();
            _source = NavimowTokenState.Hash(configuredCode, configuredRefresh);

            var state = NavimowTokenState.Parse(GetPropertyValueString(NavimowDriverFactory.TokenStateKey));
            string refresh;
            if (state != null && state.Source == _source)
            {
                refresh = state.Refresh;
                _pendingCode = null; // the code was used already
            }
            else
            {
                state = null;
                refresh = configuredRefresh;
                _pendingCode = configuredCode.Length > 0 ? configuredCode : null;
            }

            var tokenUrl = GetPropertyValueString(NavimowDriverFactory.TokenUrlKey);
            var apiUrl = GetPropertyValueString(NavimowDriverFactory.ApiUrlKey);
            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, Now,
                string.IsNullOrWhiteSpace(tokenUrl) ? NavimowDriverFactory.DefaultTokenUrl : tokenUrl.Trim(),
                GetPropertyValueString(NavimowDriverFactory.ClientIdKey)?.Trim(), GetPropertyValueString(NavimowDriverFactory.ClientSecretKey)?.Trim(), refresh);
            if (state != null)
            {
                tokens.Restore(state.Access, state.Expires);
            }
            tokens.TokenReceived += () => SaveTokenState(tokens);
            Api = new NavimowApi(Http, DriverContext.Logger, Now, string.IsNullOrWhiteSpace(apiUrl) ? NavimowDriverFactory.DefaultApiUrl : apiUrl.Trim(), tokens);
            return base.Init(token);
        }

        private void SaveTokenState(OAuthTokenClient tokens)
        {
            var json = JsonSerializer.Serialize(new NavimowTokenState
            {
                Source = _source,
                Refresh = tokens.RefreshToken,
                Access = tokens.AccessToken,
                Expires = tokens.AccessTokenExpires
            });
            var property = GetProperty(NavimowDriverFactory.TokenStateKey);
            if (property == null)
            {
                return;
            }
            property.Value = json;
            DriverContext.NodeTemplateFactory.SetPropertyValue(property.ObjId, json);
        }

        internal string LoginUrl()
        {
            var url = string.IsNullOrWhiteSpace(_loginUrl) ? NavimowDriverFactory.DefaultLoginUrl : _loginUrl;
            var query = $"response_type=code&client_id={Uri.EscapeDataString(Api.Tokens.ClientId)}&state=automatica";
            if (!string.IsNullOrWhiteSpace(_redirectUri))
            {
                query += "&redirect_uri=" + Uri.EscapeDataString(_redirectUri);
            }
            return url + (url.Contains('?') ? "&" : "?") + query;
        }

        private void SetStatus(string status)
        {
            Status = status;
            foreach (var node in _accountValues.Where(v => v.Key == NavimowDriverFactory.StatusKey))
            {
                node.Set(status);
            }
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _timer = new Timer(TimerElapsed, null, TimeSpan.Zero, _poll);
            return base.Start(token);
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
                DriverContext.Logger.LogError($"Navimow poll failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Exchanges a newly entered authorization code. False = not logged in.
        /// </summary>
        internal async Task<bool> EnsureLoginAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Api.Tokens.ClientId))
            {
                SetStatus("No client id configured (request one from Segway)");
                return false;
            }
            if (_pendingCode != null)
            {
                var code = _pendingCode;
                _pendingCode = null;
                try
                {
                    await Api.Tokens.ExchangeAuthorizationCodeAsync(code, _redirectUri, token);
                    DriverContext.Logger.LogInformation("Navimow login completed");
                }
                catch (OAuthException ex)
                {
                    SaveTokenState(Api.Tokens); // a code works only once, don't send it again after a restart
                    SetStatus($"Login failed ({ex.Message}). Open {LoginUrl()} and enter the new code");
                    return false;
                }
            }
            if (!Api.Tokens.HasCredentials)
            {
                SetStatus($"Login required: open {LoginUrl()} and enter the code from the address you are sent to");
                return false;
            }
            return true;
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                if (Api.IsBlocked || !await EnsureLoginAsync(token))
                {
                    return;
                }
                if (Now() - _devicesRead > DeviceListInterval)
                {
                    _devices = (await Api.GetDevicesAsync(token)).ToDictionary(d => d.Id);
                    _devicesRead = Now();
                }
                var ids = _mowers.Select(m => m.DeviceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
                if (ids.Count > 0)
                {
                    var status = await Api.GetStatusAsync(ids, token);
                    foreach (var mower in _mowers)
                    {
                        _devices.TryGetValue(mower.DeviceId ?? "", out var device);
                        status.TryGetValue(mower.DeviceId ?? "", out var s);
                        mower.Update(s, device);
                    }
                }
                SetStatus("OK");
            }
            catch (OAuthException ex) when (ex.IsInvalidGrant || ex.Error == "login_required")
            {
                Api.Tokens.Restore(null, DateTimeOffset.MinValue);
                SaveTokenState(Api.Tokens);
                SetStatus($"Login expired: open {LoginUrl()} and enter the new code");
            }
            catch (NavimowApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Api.Tokens.Restore(null, DateTimeOffset.MinValue);
                SaveTokenState(Api.Tokens);
                SetStatus($"Login expired: open {LoginUrl()} and enter the new code");
            }
            catch (Exception ex) when (ex is RateLimitedException or NavimowApiException or OAuthException or HttpRequestException or JsonException)
            {
                SetStatus(ex.Message);
                throw;
            }
            finally
            {
                _pollLock.Release();
            }
        }

        internal void RefreshSoon()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(CommandRefreshDelay);
                    await PollAsync();
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogDebug($"Navimow refresh after command failed: {ex.Message}");
                }
            });
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var result = new List<NodeInstance>();
            if (!await EnsureLoginAsync(token))
            {
                return result;
            }
            var known = _mowers.Select(m => m.DeviceId).ToHashSet();
            foreach (var device in await Api.GetDevicesAsync(token))
            {
                if (known.Contains(device.Id))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(NavimowDriverFactory.MowerTemplate);
                node.Name = device.Name;
                node.SetProperty(NavimowDriverFactory.DeviceIdKey, device.Id);
                foreach (var dp in NavimowDataPoints.All)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            switch (key)
            {
                case "mower":
                {
                    var mower = new NavimowMowerNode(ctx, this);
                    _mowers.Add(mower);
                    return mower;
                }
                case NavimowDriverFactory.StatusKey:
                {
                    var node = new ValueNode(ctx, key, null, null);
                    _accountValues.Add(node);
                    return node;
                }
                default:
                    return null;
            }
        }
    }

    internal sealed class NavimowMowerNode : DriverNoneAttributeBase
    {
        private readonly NavimowDriver _account;
        private readonly List<(ValueNode Node, NavimowDataPoint DataPoint)> _values = new();

        public NavimowMowerNode(IDriverContext driverContext, NavimowDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string DeviceId { get; private set; }
        internal IEnumerable<ValueNode> Values => _values.Select(v => v.Node);

        public override Task<bool> Init(CancellationToken token = default)
        {
            DeviceId = GetPropertyValueString(NavimowDriverFactory.DeviceIdKey)?.Trim();
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                DriverContext.Logger.LogError($"{FullName}: no device id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        internal void Update(JsonObject status, NavimowDevice device)
        {
            foreach (var (node, dp) in _values)
            {
                if (dp.Read?.Invoke(status, device) is { } value)
                {
                    node.Set(value);
                }
            }
        }

        private async Task WriteAsync(NavimowDataPoint dataPoint, object value, CancellationToken token)
        {
            var command = dataPoint.Write(value);
            if (command == null || string.IsNullOrWhiteSpace(DeviceId))
            {
                return;
            }
            if (!await _account.EnsureLoginAsync(token))
            {
                return;
            }
            DriverContext.Logger.LogInformation($"{DeviceId}: {command.Command} {JsonSerializer.Serialize(command.Params)}");
            await _account.Api.SendCommandAsync(DeviceId, command.Command, command.Params, token);
            _account.RefreshSoon();
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _account.PollAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = NavimowDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new ValueNode(ctx, key, dataPoint.IsWriteable ? (v, t) => WriteAsync(dataPoint, v, t) : null, null);
            _values.Add((node, dataPoint));
            return node;
        }
    }
}
