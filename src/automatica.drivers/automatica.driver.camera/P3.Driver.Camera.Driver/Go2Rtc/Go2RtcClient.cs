using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Camera.Driver.Go2Rtc
{
    /// <summary>
    /// The streams API of go2rtc (https://github.com/AlexxIT/go2rtc): streams are created at runtime,
    /// so the go2rtc configuration needs no camera entries.
    /// </summary>
    internal sealed class Go2RtcClient
    {
        private readonly HttpClient _http;
        private readonly Uri _baseUri;
        private readonly AuthenticationHeaderValue _auth;

        public Go2RtcClient(HttpClient http, string baseUrl, string username, string password)
        {
            _http = http;
            var url = string.IsNullOrWhiteSpace(baseUrl) ? CameraDriverFactory.DefaultGo2RtcUrl : baseUrl.Trim();
            _baseUri = new Uri(url.EndsWith('/') ? url : url + "/");
            if (!string.IsNullOrEmpty(username))
            {
                _auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
            }
        }

        public Uri BaseUri => _baseUri;

        /// <summary>
        /// Names of the streams go2rtc knows
        /// </summary>
        public async Task<IReadOnlySet<string>> GetStreamNamesAsync(CancellationToken token = default)
        {
            using var request = Create(HttpMethod.Get, "api/streams");
            using var response = await _http.SendAsync(request, token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new HashSet<string>();
            }
            return json.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>
        /// Creates the stream or replaces its source. A PUT for an existing name does not reliably replace the source
        /// across go2rtc versions, so the stream is removed first.
        /// </summary>
        public async Task SetStreamAsync(string name, string source, CancellationToken token = default)
        {
            await DeleteStreamAsync(name, token);

            using var request = Create(HttpMethod.Put, $"api/streams?name={Uri.EscapeDataString(name)}&src={Uri.EscapeDataString(source)}");
            using var response = await _http.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(token);
                throw new HttpRequestException($"go2rtc rejected stream {name}: {(int)response.StatusCode} {Truncate(body)}", null, response.StatusCode);
            }
        }

        /// <summary>
        /// Removes the stream, a stream that does not exist is fine
        /// </summary>
        public async Task DeleteStreamAsync(string name, CancellationToken token = default)
        {
            using var request = Create(HttpMethod.Delete, $"api/streams?src={Uri.EscapeDataString(name)}");
            using var response = await _http.SendAsync(request, token);
            // go2rtc answers unknown names with an error status, nothing to clean up then
        }

        private HttpRequestMessage Create(HttpMethod method, string relative)
        {
            var request = new HttpRequestMessage(method, new Uri(_baseUri, relative));
            if (_auth != null)
            {
                request.Headers.Authorization = _auth;
            }
            return request;
        }

        private static string Truncate(string s) => s == null || s.Length <= 200 ? s : s[..200];
    }
}
