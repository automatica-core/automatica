using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Http.Driver.Extraction;
using P3.Driver.Http.Driver.Security;

namespace P3.Driver.Http.Driver
{
    /// <summary>
    /// Virtual HTTP input: polls a URL and extracts one value per command from the response
    /// </summary>
    internal sealed class InputDeviceNode : DriverNoneAttributeBase
    {
        private readonly HttpDriver _driver;
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private readonly List<InputCommandNode> _commands = new();
        private readonly List<ValueNode> _online = new();

        private Timer _timer;
        private Uri _uri;
        private TimeSpan _poll;
        private TimeSpan _timeout;
        private Credentials _credentials;
        private bool? _lastOnline;
        private string _lastError;

        public InputDeviceNode(IDriverContext driverContext, HttpDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        internal IReadOnlyList<InputCommandNode> Commands => _commands;

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(HttpDriverFactory.PollKey), 10, 604800));
            _timeout = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(HttpDriverFactory.TimeoutKey), 1, 60));
            _credentials = new Credentials(GetPropertyValueString(HttpDriverFactory.UserKey), GetPropertyValueString(HttpDriverFactory.PasswordKey));

            try
            {
                _uri = RequestGuard.ValidateUrl(GetPropertyValueString(HttpDriverFactory.UrlKey));
                if (_credentials.IsSet && HttpLog.IsPlainHttp(_uri))
                {
                    DriverContext.Logger.LogWarning(HttpLog.PlainHttpWarning(Name, _uri));
                }
            }
            catch (RequestBlockedException ex)
            {
                _uri = null;
                Error = ex.Message;
                DriverContext.Logger.LogError($"{Name}: {ex.Message}, the input is not polled");
            }

            return base.Init(token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            if (_uri != null)
            {
                DriverContext.Logger.LogInformation($"{Name}: polling {RequestGuard.ForLog(_uri)} every {_poll.TotalSeconds}s");
                _timer = new Timer(TimerElapsed, null, TimeSpan.Zero, _poll);
            }
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
                DriverContext.Logger.LogError(ex, $"{Name}: polling failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        /// <summary>
        /// Requests the URL once and updates every command. A poll that is still running is not started twice.
        /// </summary>
        internal async Task PollAsync(CancellationToken token = default)
        {
            if (_uri == null || !await _pollLock.WaitAsync(0, token))
            {
                return;
            }
            try
            {
                var result = await _driver.SendAsync(HttpMethod.Get, _uri, null, null, _credentials, _timeout, true, token);
                SetOnline(result.Success, result.Error);
                if (!result.Success)
                {
                    return;
                }

                foreach (var command in _commands)
                {
                    command.Apply(result.Body);
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        private void SetOnline(bool online, string error)
        {
            // log only the changes, a device that is off would fill the log with every poll
            if (!online && (_lastOnline != false || error != _lastError))
            {
                DriverContext.Logger.LogWarning($"{Name}: request to {RequestGuard.ForLog(_uri)} failed: {error}");
            }
            else if (online && _lastOnline == false)
            {
                DriverContext.Logger.LogInformation($"{Name}: {RequestGuard.ForLog(_uri)} answers again");
            }
            _lastOnline = online;
            _lastError = error;
            Error = error;

            foreach (var node in _online)
            {
                node.Set(online);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "online":
                    var online = new ValueNode(ctx);
                    _online.Add(online);
                    return online;
                case "value":
                case "digital":
                case "text":
                    var command = new InputCommandNode(ctx, ctx.NodeInstance.This2NodeTemplateNavigation.Key);
                    _commands.Add(command);
                    return command;
            }
            return null;
        }
    }

    /// <summary>
    /// One value of a virtual HTTP input (Loxone: command)
    /// </summary>
    internal sealed class InputCommandNode : DriverNotWriteableBase
    {
        private readonly string _kind;
        private readonly object _lock = new();
        private ExtractMode _mode;
        private string _pattern;
        private bool _hasValue;
        private string _lastProblem;

        public object LastValue { get; private set; }

        public InputCommandNode(IDriverContext driverContext, string kind) : base(driverContext)
        {
            _kind = kind;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var mode = GetPropertyValueInt(HttpDriverFactory.ModeKey);
            _mode = Enum.IsDefined(typeof(ExtractMode), mode) ? (ExtractMode)mode : ExtractMode.Json;
            _pattern = GetPropertyValueString(HttpDriverFactory.PatternKey) ?? "";
            return base.Init(token);
        }

        /// <summary>
        /// Extracts the value from <paramref name="body"/> and dispatches it when it changed
        /// </summary>
        internal void Apply(string body)
        {
            string text;
            try
            {
                text = ValueExtractor.Extract(body, _mode, _pattern);
            }
            catch (Exception ex) when (ex is FormatException or JsonException or RegexMatchTimeoutException)
            {
                Problem(ex is JsonException ? $"the response is not valid JSON ({ex.Message})" : ex.Message);
                return;
            }

            object value = _kind switch
            {
                "value" => ValueExtractor.ToNumber(text),
                "digital" => ValueExtractor.ToBool(text),
                _ => text
            };

            if (value == null)
            {
                Problem(text == null ? $"\"{_pattern}\" was not found in the response" : $"\"{HttpLog.Trim(text, 40)}\" is not a {(_kind == "digital" ? "digital value" : "number")}");
                return;
            }
            Problem(null);

            lock (_lock)
            {
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }
                _hasValue = true;
                LastValue = value;
            }
            DispatchRead(value);
        }

        private void Problem(string problem)
        {
            if (problem != null && problem != _lastProblem)
            {
                DriverContext.Logger.LogWarning($"{Name}: {problem}");
            }
            _lastProblem = problem;
            Error = problem;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            if (_hasValue)
            {
                DispatchRead(LastValue);
            }
            return Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }

    /// <summary>
    /// A read-only status data point that only dispatches when its value changes
    /// </summary>
    internal sealed class ValueNode : DriverNotWriteableBase
    {
        private readonly object _lock = new();
        private bool _hasValue;

        public object LastValue { get; private set; }

        public ValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void Set(object value)
        {
            lock (_lock)
            {
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }
                _hasValue = true;
                LastValue = value;
            }
            DispatchRead(value);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            if (_hasValue)
            {
                DispatchRead(LastValue);
            }
            return Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
