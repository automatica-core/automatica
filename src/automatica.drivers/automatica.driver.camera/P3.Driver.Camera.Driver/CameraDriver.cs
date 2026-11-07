using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.Camera.Driver.Go2Rtc;
using P3.Driver.Camera.Driver.Onvif;

namespace P3.Driver.Camera.Driver
{
    /// <summary>
    /// Keeps the streams of all cameras registered in go2rtc (it forgets runtime streams when it restarts,
    /// so every poll compares) and polls the online state. The scan finds ONVIF cameras on the local network.
    /// </summary>
    internal sealed class CameraDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5)
        })
        {
            // the calls carry their own timeouts, the ONVIF pull is a long poll
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        private readonly List<CameraNode> _cameras = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);

        private Timer _timer;
        private TimeSpan _poll;
        private bool _go2RtcDown;

        public CameraDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        internal HttpClient Http { get; set; } = SharedHttp;

        internal Go2RtcClient Go2Rtc { get; private set; }

        internal IOnvifDiscovery Discovery { get; set; } = new OnvifDiscovery();

        internal Func<string, int, CancellationToken, Task<bool>> OnlineProbe { get; set; } = TcpProbeAsync;

        internal TimeSpan EventRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        internal TimeSpan DiscoveryDuration { get; set; } = TimeSpan.FromSeconds(4);

        internal IReadOnlyList<CameraNode> Cameras => _cameras;

        internal DateTimeOffset Now => DriverContext.TimeProvider.GetUtcNow();

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(10, GetPropertyValueInt(CameraDriverFactory.PollKey)));

            var url = GetPropertyValueString(CameraDriverFactory.Go2RtcUrlKey);
            try
            {
                Go2Rtc = new Go2RtcClient(Http, url, GetPropertyValueString(CameraDriverFactory.Go2RtcUsernameKey),
                    GetPropertyValueString(CameraDriverFactory.Go2RtcPasswordKey));
            }
            catch (UriFormatException)
            {
                DriverContext.Logger.LogError($"\"{url}\" is not a valid go2rtc URL, using {CameraDriverFactory.DefaultGo2RtcUrl}");
                Go2Rtc = new Go2RtcClient(Http, CameraDriverFactory.DefaultGo2RtcUrl, null, null);
            }

            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            DriverContext.Logger.LogInformation($"{_cameras.Count} camera(s), go2rtc at {Go2Rtc.BaseUri}, polling every {_poll.TotalSeconds}s");

            // ONVIF can take a while, the first poll must not hold up the start of the server
            _timer = new Timer(TimerElapsed, null, TimeSpan.Zero, _poll);
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
                DriverContext.Logger.LogError(ex, "Camera poll failed");
            }
        }

        /// <summary>
        /// Polls every camera. Concurrent calls wait for the running poll.
        /// </summary>
        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                IReadOnlySet<string> existing = null;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    existing = await Go2Rtc.GetStreamNamesAsync(cts.Token);
                    if (_go2RtcDown)
                    {
                        DriverContext.Logger.LogInformation($"go2rtc at {Go2Rtc.BaseUri} is reachable again");
                    }
                    _go2RtcDown = false;
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException && !token.IsCancellationRequested)
                {
                    if (!_go2RtcDown)
                    {
                        DriverContext.Logger.LogWarning($"go2rtc at {Go2Rtc.BaseUri} is not reachable ({ex.Message}), the streams are registered once it is");
                    }
                    _go2RtcDown = true;
                }

                await Task.WhenAll(_cameras.Select(async camera =>
                {
                    try
                    {
                        await camera.PollAsync(existing, token);
                    }
                    catch (Exception ex) when (!token.IsCancellationRequested)
                    {
                        DriverContext.Logger.LogError(ex, $"{camera.FullName}: poll failed");
                    }
                }));
            }
            finally
            {
                _pollLock.Release();
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        /// <summary>
        /// ONVIF cameras on the local network that are not configured yet
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var configured = _cameras.Select(c => c.Host).Where(h => !string.IsNullOrEmpty(h)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<NodeInstance>();

            foreach (var camera in await Discovery.ProbeAsync(DiscoveryDuration, token))
            {
                if (configured.Contains(camera.Host))
                {
                    continue;
                }

                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(CameraDriverFactory.CameraTemplate);
                var label = string.Join(" ", new[] { camera.Name, camera.Hardware }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase));
                node.Name = string.IsNullOrEmpty(label) ? camera.Host : $"{label} ({camera.Host})";
                node.SetProperty(CameraDriverFactory.HostKey, camera.Host);
                node.SetProperty(CameraDriverFactory.OnvifPortKey, camera.Port);
                foreach (var template in CameraDriverFactory.CameraValueTemplates)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(template));
                }
                result.Add(node);
            }

            DriverContext.Logger.LogInformation($"Scan found {result.Count} new ONVIF camera(s)");
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key == "camera")
            {
                var camera = new CameraNode(ctx, this);
                _cameras.Add(camera);
                return camera;
            }
            return null;
        }

        private static async Task<bool> TcpProbeAsync(string host, int port, CancellationToken token)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, cts.Token);
                return true;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException && !token.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
