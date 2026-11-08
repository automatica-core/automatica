using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Http.Driver.Security;

namespace P3.Driver.Http.Driver
{
    /// <summary>
    /// Virtual HTTP output: the address and credentials of a device, its commands send the requests
    /// </summary>
    internal sealed class OutputDeviceNode : DriverNoneAttributeBase
    {
        private readonly HttpDriver _driver;
        private readonly List<ValueNode> _status = new();
        private bool _warnedPlainHttp;

        public OutputDeviceNode(IDriverContext driverContext, HttpDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        internal string Address { get; private set; }
        internal TimeSpan Timeout { get; private set; }
        internal Credentials Credentials { get; private set; }

        public override Task<bool> Init(CancellationToken token = default)
        {
            Address = GetPropertyValueString(HttpDriverFactory.AddressKey) ?? "";
            Timeout = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(HttpDriverFactory.TimeoutKey), 1, 60));
            Credentials = new Credentials(GetPropertyValueString(HttpDriverFactory.UserKey), GetPropertyValueString(HttpDriverFactory.PasswordKey));
            return base.Init(token);
        }

        internal async Task<HttpResult> SendAsync(string commandName, HttpMethod method, string command, string body, string contentType, CancellationToken token)
        {
            HttpResult result;
            Uri uri = null;
            try
            {
                uri = RequestGuard.Combine(Address, command);
                if (Credentials.IsSet && HttpLog.IsPlainHttp(uri) && !_warnedPlainHttp)
                {
                    _warnedPlainHttp = true;
                    DriverContext.Logger.LogWarning(HttpLog.PlainHttpWarning(Name, uri));
                }
                result = await _driver.SendAsync(method, uri, body, contentType, Credentials, Timeout, false, token);
            }
            catch (RequestBlockedException ex)
            {
                result = new HttpResult(0, null, ex.Message);
            }

            if (!result.Success)
            {
                DriverContext.Logger.LogWarning($"{Name} / {commandName}: {method} {RequestGuard.ForLog(uri)} failed: {result.Error}");
            }
            foreach (var node in _status)
            {
                node.Set(result.Status);
            }
            return result;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "status":
                    var status = new ValueNode(ctx);
                    _status.Add(status);
                    return status;
                case "command":
                    return new OutputCommandNode(ctx, this, false);
                case "digital_command":
                    return new OutputCommandNode(ctx, this, true);
            }
            return null;
        }
    }

    /// <summary>
    /// One command of a virtual HTTP output. A value other than 0 (or true) sends the ON command, 0 (false) the OFF command.
    /// An analog command without an OFF command sends the ON command for 0 as well, a digital command sends nothing then.
    /// &lt;v&gt; in the command and the body is replaced by the value (digital: 1 or 0), &lt;v.2&gt; with two decimals.
    /// </summary>
    internal sealed class OutputCommandNode : DriverNotReadableBase
    {
        private static readonly Regex Placeholder = new(@"<v(?:\.(\d))?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly OutputDeviceNode _device;
        private readonly bool _digital;
        private CommandMethod _method;
        private string _commandOn;
        private string _bodyOn;
        private string _commandOff;
        private string _bodyOff;
        private string _contentType;

        public OutputCommandNode(IDriverContext driverContext, OutputDeviceNode device, bool digital) : base(driverContext)
        {
            _device = device;
            _digital = digital;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var method = GetPropertyValueInt(HttpDriverFactory.MethodKey);
            _method = Enum.IsDefined(typeof(CommandMethod), method) ? (CommandMethod)method : CommandMethod.Get;
            _commandOn = GetPropertyValueString(HttpDriverFactory.CommandOnKey) ?? "";
            _bodyOn = GetPropertyValueString(HttpDriverFactory.BodyOnKey) ?? "";
            _commandOff = GetPropertyValueString(HttpDriverFactory.CommandOffKey) ?? "";
            _bodyOff = GetPropertyValueString(HttpDriverFactory.BodyOffKey) ?? "";
            _contentType = GetPropertyValueString(HttpDriverFactory.ContentTypeKey);
            if (string.IsNullOrWhiteSpace(_contentType))
            {
                _contentType = HttpDriverFactory.DefaultContentType;
            }
            return base.Init(token);
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (await SendAsync(value, token) is { Success: true })
            {
                await writeContext.DispatchValue(value, token);
            }
        }

        /// <summary>
        /// Sends the command for <paramref name="value"/>, null when nothing is configured for it
        /// </summary>
        internal async Task<HttpResult?> SendAsync(object value, CancellationToken token = default)
        {
            var number = ToNumber(value);
            if (number == null)
            {
                DriverContext.Logger.LogWarning($"{Name}: cannot send \"{value}\", it is not a number");
                return null;
            }

            var on = number.Value != 0;
            var useOff = !on && (_commandOff.Length > 0 || _bodyOff.Length > 0);
            if (!on && !useOff && _digital)
            {
                return null;
            }

            var command = Replace(useOff ? _commandOff : _commandOn, number.Value);
            var body = Replace(useOff ? _bodyOff : _bodyOn, number.Value);
            var method = _method switch
            {
                CommandMethod.Post => HttpMethod.Post,
                CommandMethod.Put => HttpMethod.Put,
                _ => HttpMethod.Get
            };

            return await _device.SendAsync(Name, method, command, method == HttpMethod.Get ? null : body, _contentType, token);
        }

        internal string Replace(string template, double value)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }
            return Placeholder.Replace(template, m =>
            {
                if (_digital)
                {
                    return value != 0 ? "1" : "0";
                }
                return m.Groups[1].Success
                    ? value.ToString("F" + m.Groups[1].Value, CultureInfo.InvariantCulture)
                    : value.ToString("R", CultureInfo.InvariantCulture);
            });
        }

        private static double? ToNumber(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return Extraction.ValueExtractor.ToNumber(s);
                default:
                    try
                    {
                        var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        return null;
                    }
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
