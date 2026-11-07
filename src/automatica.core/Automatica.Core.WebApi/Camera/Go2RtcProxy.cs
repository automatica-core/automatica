using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.WebApi.Camera
{
    /// <summary>
    /// Where go2rtc runs. server:go2rtc_url points to an own go2rtc (e.g. a container), without it the server
    /// starts the installed go2rtc binary on 127.0.0.1:1984 (<see cref="Go2RtcProcessHost"/>).
    /// </summary>
    public sealed class Go2RtcSettings
    {
        public const string DefaultUrl = "http://127.0.0.1:1984";
        public const int ManagedPort = 1984;

        public Go2RtcSettings(IConfiguration configuration)
        {
            var url = configuration?["server:go2rtc_url"];
            IsExternal = !string.IsNullOrWhiteSpace(url);
            var baseUrl = IsExternal ? url.Trim() : DefaultUrl;
            BaseUri = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            Username = configuration?["server:go2rtc_username"];
            Password = configuration?["server:go2rtc_password"];
            BinaryPath = configuration?["server:go2rtc_path"];
        }

        public Uri BaseUri { get; }

        /// <summary>
        /// True when server:go2rtc_url is set: the server does not start go2rtc itself
        /// </summary>
        public bool IsExternal { get; }

        public string Username { get; }

        public string Password { get; }

        public string BinaryPath { get; }
    }

    /// <summary>
    /// The calls of the camera endpoint to go2rtc. The camera driver registers the streams under the camera node id
    /// ("{id}", "{id}_sub", "{id}_snap"), see P3.Driver.Camera.Driver.CameraStreams.
    /// </summary>
    public class Go2RtcProxy
    {
        private static readonly HttpClient SharedHttp = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private readonly HttpClient _http;
        private readonly AuthenticationHeaderValue _auth;

        public Go2RtcProxy(Go2RtcSettings settings) : this(settings, SharedHttp)
        {
        }

        public Go2RtcProxy(Go2RtcSettings settings, HttpClient http)
        {
            Settings = settings;
            _http = http;
            if (!string.IsNullOrEmpty(settings.Username))
            {
                _auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}")));
            }
        }

        public Go2RtcSettings Settings { get; }

        public static string MainStream(Guid cameraId) => cameraId.ToString("D");
        public static string SubStream(Guid cameraId) => cameraId.ToString("D") + "_sub";
        public static string SnapshotStream(Guid cameraId) => cameraId.ToString("D") + "_snap";

        public virtual async Task<IReadOnlySet<string>> GetStreamNamesAsync(CancellationToken token)
        {
            using var request = Create(HttpMethod.Get, "api/streams");
            using var response = await _http.SendAsync(request, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            return json.RootElement.ValueKind == JsonValueKind.Object
                ? json.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>();
        }

        /// <summary>
        /// A JPEG of the stream, null if go2rtc has none (an H.264 stream needs ffmpeg next to go2rtc for this)
        /// </summary>
        public virtual async Task<byte[]> GetFrameAsync(string stream, CancellationToken token)
        {
            using var request = Create(HttpMethod.Get, $"api/frame.jpeg?src={Uri.EscapeDataString(stream)}");
            using var response = await _http.SendAsync(request, token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "image/jpeg")
            {
                return null;
            }
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            return bytes.Length > 0 ? bytes : null;
        }

        /// <summary>
        /// Opens the go2rtc WebSocket of the stream (MSE / MJPEG protocol of go2rtc)
        /// </summary>
        public virtual async Task<WebSocket> ConnectAsync(string stream, CancellationToken token)
        {
            var builder = new UriBuilder(new Uri(Settings.BaseUri, "api/ws"))
            {
                Scheme = Settings.BaseUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
                Query = "src=" + Uri.EscapeDataString(stream)
            };

            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            if (_auth != null)
            {
                socket.Options.SetRequestHeader("Authorization", _auth.ToString());
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await socket.ConnectAsync(builder.Uri, cts.Token);
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Is a go2rtc API answering?
        /// </summary>
        public virtual async Task<bool> IsAvailableAsync(CancellationToken token)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                using var request = Create(HttpMethod.Get, "api");
                using var response = await _http.SendAsync(request, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException && !token.IsCancellationRequested)
            {
                return false;
            }
        }

        private HttpRequestMessage Create(HttpMethod method, string relative)
        {
            var request = new HttpRequestMessage(method, new Uri(Settings.BaseUri, relative));
            if (_auth != null)
            {
                request.Headers.Authorization = _auth;
            }
            return request;
        }
    }
}
