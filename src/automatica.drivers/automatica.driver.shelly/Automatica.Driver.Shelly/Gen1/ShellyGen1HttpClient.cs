#nullable enable annotations
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Rpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.Gen1
{
    /// <summary>
    /// HTTP client of a Gen1 device: GET /shelly, /settings, /status, /cit/d and the control URLs (/relay/0?turn=on...).
    /// Restricted login is HTTP basic auth.
    /// https://shelly-api-docs.shelly.cloud/gen1/#common-http-api
    /// </summary>
    public sealed class ShellyGen1HttpClient : IDisposable
    {
        private readonly string _address;
        private readonly ITelegramMonitorInstance _monitor;
        private readonly HttpClient _http;
        private readonly AuthenticationHeaderValue? _auth;

        public TimeSpan Timeout { get; }

        public ShellyGen1HttpClient(string address, string? userName, string? password, ITelegramMonitorInstance? monitor = null, TimeSpan? timeout = null)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            _monitor = monitor ?? new EmptyTelegramMonitorInstance();
            Timeout = timeout ?? TimeSpan.FromSeconds(5);
            _http = new HttpClient { BaseAddress = new Uri($"http://{address}") };

            if (!string.IsNullOrEmpty(password))
            {
                var user = string.IsNullOrEmpty(userName) ? "admin" : userName;
                _auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
            }
        }

        /// <summary>
        /// GET <paramref name="pathAndQuery"/> and parse the JSON answer. Throws <see cref="ShellyRpcException"/> with the HTTP status as code.
        /// </summary>
        public async Task<JToken> GetJsonAsync(string pathAndQuery, CancellationToken token = default, TimeSpan? timeout = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, pathAndQuery);
            if (_auth != null)
            {
                request.Headers.Authorization = _auth;
            }

            var effectiveTimeout = timeout ?? Timeout;
            using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

            await _monitor.NotifyTelegram(TelegramDirection.Output, "self", _address, "GET", pathAndQuery);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, linked.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                throw new ShellyRpcException(408, $"{_address} did not answer {pathAndQuery} within {effectiveTimeout.TotalSeconds:0.#} s");
            }
            catch (HttpRequestException e)
            {
                throw new ShellyRpcException(503, $"{_address} is not reachable: {e.Message}");
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync(linked.Token);
                await _monitor.NotifyTelegram(TelegramDirection.Input, _address, "self", text, pathAndQuery);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ShellyRpcException(401, _auth == null
                        ? $"{_address} requires a login, set user name and password of the device"
                        : $"{_address} rejected user name or password");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new ShellyRpcException((int)response.StatusCode, $"{_address} answered {pathAndQuery} with HTTP {(int)response.StatusCode} {text}".Trim());
                }

                try
                {
                    return JToken.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
                }
                catch (JsonException)
                {
                    throw new ShellyRpcException(500, $"{_address} answered {pathAndQuery} with no JSON");
                }
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
