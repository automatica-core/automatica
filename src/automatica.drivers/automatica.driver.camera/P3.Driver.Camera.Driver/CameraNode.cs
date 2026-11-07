using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Camera.Driver.Go2Rtc;
using P3.Driver.Camera.Driver.Onvif;

namespace P3.Driver.Camera.Driver
{
    /// <summary>
    /// The names of the go2rtc streams of a camera node. The core camera endpoint
    /// (webapi/camera/{nodeId}) uses the same names, keep them in sync.
    /// </summary>
    public static class CameraStreams
    {
        public static string Main(Guid cameraNodeId) => cameraNodeId.ToString("D");
        public static string Sub(Guid cameraNodeId) => cameraNodeId.ToString("D") + "_sub";
        public static string Snapshot(Guid cameraNodeId) => cameraNodeId.ToString("D") + "_snap";
    }

    /// <summary>
    /// One camera: resolves its stream URLs (configured or via ONVIF), keeps them registered in go2rtc,
    /// probes the online state and follows the ONVIF motion / person events.
    /// </summary>
    internal sealed class CameraNode : DriverNoneAttributeBase
    {
        internal static readonly TimeSpan SubscriptionTermination = TimeSpan.FromSeconds(120);
        internal static readonly TimeSpan RenewInterval = TimeSpan.FromSeconds(60);
        internal static readonly TimeSpan PullTimeout = TimeSpan.FromSeconds(10);

        private readonly CameraDriver _driver;
        private readonly List<ValueNode> _online = new();
        private readonly List<ValueNode> _motion = new();
        private readonly List<ValueNode> _person = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private readonly HashSet<string> _registered = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _motionStates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _personStates = new(StringComparer.Ordinal);

        private string _host;
        private int _onvifPort;
        private string _username;
        private string _password;
        private bool _useOnvif;
        private bool _events;
        private string _configuredMain;
        private string _configuredSub;
        private string _configuredSnapshot;

        private OnvifClient _onvif;
        private bool _resolved;
        private DateTimeOffset _nextResolve = DateTimeOffset.MinValue;
        private CancellationTokenSource _stop;
        private Task _eventLoop;
        private bool _warnedNoStream;

        public CameraNode(IDriverContext driverContext, CameraDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        /// <summary>
        /// go2rtc stream name → source
        /// </summary>
        internal IReadOnlyDictionary<string, string> Sources { get; private set; } = new Dictionary<string, string>();

        internal bool? Online { get; private set; }

        internal bool EventsRunning => _eventLoop is { IsCompleted: false };

        internal string Host => _host;

        public override Task<bool> Init(CancellationToken token = default)
        {
            _host = GetPropertyValueString(CameraDriverFactory.HostKey)?.Trim();
            _onvifPort = GetPropertyValueInt(CameraDriverFactory.OnvifPortKey);
            if (_onvifPort <= 0)
            {
                _onvifPort = 80;
            }
            _username = GetPropertyValueString(CameraDriverFactory.UsernameKey)?.Trim();
            _password = GetPropertyValueString(CameraDriverFactory.PasswordKey);
            _useOnvif = GetPropertyValueBool(CameraDriverFactory.UseOnvifKey) ?? true;
            _events = GetPropertyValueBool(CameraDriverFactory.EventsKey) ?? true;
            _configuredMain = Blank(GetPropertyValueString(CameraDriverFactory.StreamMainKey));
            _configuredSub = Blank(GetPropertyValueString(CameraDriverFactory.StreamSubKey));
            _configuredSnapshot = Blank(GetPropertyValueString(CameraDriverFactory.SnapshotUrlKey));

            _resolved = false;
            _registered.Clear();
            Sources = new Dictionary<string, string>();

            if (_configuredMain == null && (!_useOnvif || string.IsNullOrEmpty(_host)))
            {
                DriverContext.Logger.LogWarning($"{FullName}: no main stream URL and no ONVIF host, the camera has nothing to show");
            }

            return base.Init(token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _stop?.Cancel();
            _stop = new CancellationTokenSource();
            return base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            var stop = _stop;
            _stop = null;
            stop?.Cancel();

            if (_eventLoop != null)
            {
                try
                {
                    await _eventLoop.WaitAsync(TimeSpan.FromSeconds(5), token);
                }
                catch (Exception)
                {
                    // the loop ends on its own
                }
                _eventLoop = null;
            }

            await RemoveStreamsAsync();
            return await base.Stop(token);
        }

        /// <summary>
        /// One poll: resolve the URLs if needed, (re-)register the streams go2rtc is missing, probe the online state,
        /// keep the event loop running.
        /// </summary>
        /// <param name="existing">the streams go2rtc has, null if go2rtc is not reachable</param>
        internal async Task PollAsync(IReadOnlySet<string> existing, CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                if (!_resolved && _driver.Now >= _nextResolve)
                {
                    await ResolveAsync(token);
                }

                if (existing != null && _driver.Go2Rtc != null)
                {
                    await RegisterAsync(existing, token);
                }

                await ProbeOnlineAsync(token);
                EnsureEventLoop();
            }
            finally
            {
                _pollLock.Release();
            }
        }

        private async Task ResolveAsync(CancellationToken token)
        {
            string main = _configuredMain, sub = _configuredSub, snapshot = _configuredSnapshot;

            var needsOnvif = _useOnvif && !string.IsNullOrEmpty(_host) && (main == null || sub == null || snapshot == null || _events);
            if (needsOnvif)
            {
                try
                {
                    _onvif ??= new OnvifClient(_driver.Http, _host, _onvifPort, _username, _password, () => _driver.Now);
                    using var cts = Timeout(token, TimeSpan.FromSeconds(20));
                    await _onvif.ConnectAsync(cts.Token);
                    if (main == null || sub == null || snapshot == null)
                    {
                        var streams = await _onvif.GetStreamsAsync(cts.Token);
                        main ??= streams.MainUri;
                        sub ??= streams.SubUri;
                        snapshot ??= streams.SnapshotUri;
                    }
                }
                catch (Exception ex) when (ex is OnvifException or OperationCanceledException or HttpRequestException && !token.IsCancellationRequested)
                {
                    _onvif = null;
                    if (main == null)
                    {
                        // without a stream there is nothing to register, ask again later
                        _nextResolve = _driver.Now + TimeSpan.FromMinutes(1);
                        DriverContext.Logger.LogWarning($"{FullName}: ONVIF on {_host}:{_onvifPort} failed ({ex.Message}), retrying in a minute");
                        return;
                    }
                    DriverContext.Logger.LogInformation($"{FullName}: ONVIF on {_host}:{_onvifPort} failed ({ex.Message}), using the configured URLs only");
                }
            }

            var sources = new Dictionary<string, string>(StringComparer.Ordinal);
            var mainSource = StreamUrl.ToGo2RtcSource(StreamUrl.WithCredentials(main, _username, _password));
            if (mainSource != null)
            {
                sources[CameraStreams.Main(Id)] = mainSource;
            }
            var subSource = StreamUrl.ToGo2RtcSource(StreamUrl.WithCredentials(sub, _username, _password));
            if (subSource != null && subSource != mainSource)
            {
                sources[CameraStreams.Sub(Id)] = subSource;
            }
            var snapshotSource = StreamUrl.WithCredentials(snapshot, _username, _password);
            if (snapshotSource != null)
            {
                sources[CameraStreams.Snapshot(Id)] = snapshotSource;
            }

            if (mainSource == null && !_warnedNoStream)
            {
                _warnedNoStream = true;
                DriverContext.Logger.LogWarning($"{FullName}: no main stream URL found (set it or check the ONVIF settings)");
            }

            Sources = sources;
            _registered.Clear();
            _resolved = true;
            DriverContext.Logger.LogInformation($"{FullName}: streams {string.Join(", ", sources.Select(s => $"{s.Key} = {StreamUrl.Redact(s.Value)}"))}");
        }

        private async Task RegisterAsync(IReadOnlySet<string> existing, CancellationToken token)
        {
            foreach (var (name, source) in Sources)
            {
                // a stream of an older run may still have an old source, so each source is set once per run
                if (_registered.Contains(name) && existing.Contains(name))
                {
                    continue;
                }

                try
                {
                    await _driver.Go2Rtc.SetStreamAsync(name, source, token);
                    _registered.Add(name);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
                {
                    DriverContext.Logger.LogWarning($"{FullName}: registering stream {name} in go2rtc failed: {ex.Message}");
                }
            }
        }

        private async Task RemoveStreamsAsync()
        {
            if (_driver.Go2Rtc == null || _registered.Count == 0)
            {
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            foreach (var name in _registered.ToList())
            {
                try
                {
                    await _driver.Go2Rtc.DeleteStreamAsync(name, cts.Token);
                }
                catch (Exception)
                {
                    // go2rtc may be gone already
                }
            }
            _registered.Clear();
        }

        private async Task ProbeOnlineAsync(CancellationToken token)
        {
            string host;
            int port;
            if (!(Sources.TryGetValue(CameraStreams.Main(Id), out var main) && StreamUrl.TryGetEndpoint(main, 554, out host, out port)))
            {
                if (string.IsNullOrEmpty(_host))
                {
                    return;
                }
                host = _host;
                port = _useOnvif ? _onvifPort : 554;
            }

            var online = await _driver.OnlineProbe(host, port, token);
            if (Online != online)
            {
                DriverContext.Logger.LogInformation($"{FullName}: {(online ? "online" : "offline")} ({host}:{port})");
            }
            Online = online;
            foreach (var node in _online)
            {
                node.Set(online);
            }
        }

        #region events

        private void EnsureEventLoop()
        {
            if (!_events || !_useOnvif || _onvif == null || _stop == null || EventsRunning)
            {
                return;
            }

            var token = _stop.Token;
            _eventLoop = Task.Run(() => EventLoopAsync(token), token);
        }

        private async Task EventLoopAsync(CancellationToken token)
        {
            var failures = 0;
            while (!token.IsCancellationRequested)
            {
                Uri subscription = null;
                try
                {
                    var onvif = _onvif;
                    if (onvif == null)
                    {
                        return;
                    }
                    if (onvif.EventServiceUri == null)
                    {
                        DriverContext.Logger.LogInformation($"{FullName}: the camera has no ONVIF event service, no motion events");
                        return;
                    }

                    using (var cts = Timeout(token, TimeSpan.FromSeconds(15)))
                    {
                        subscription = await onvif.CreatePullPointSubscriptionAsync(SubscriptionTermination, cts.Token);
                    }
                    DriverContext.Logger.LogInformation($"{FullName}: following the ONVIF events");
                    failures = 0;

                    var renewAt = _driver.Now + RenewInterval;
                    while (!token.IsCancellationRequested)
                    {
                        IReadOnlyList<OnvifEvent> events;
                        using (var cts = Timeout(token, PullTimeout + TimeSpan.FromSeconds(15)))
                        {
                            events = await onvif.PullMessagesAsync(subscription, PullTimeout, 50, cts.Token);
                        }
                        ApplyEvents(events);

                        if (_driver.Now >= renewAt)
                        {
                            using var cts = Timeout(token, TimeSpan.FromSeconds(15));
                            await onvif.RenewAsync(subscription, SubscriptionTermination, cts.Token);
                            renewAt = _driver.Now + RenewInterval;
                        }
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    failures++;
                    var level = failures == 1 ? LogLevel.Warning : LogLevel.Debug;
                    DriverContext.Logger.Log(level, $"{FullName}: ONVIF events failed ({ex.Message}), retrying");
                    if (ex is OnvifException { IsNotAuthorized: true } && failures >= 3)
                    {
                        DriverContext.Logger.LogWarning($"{FullName}: the camera refuses the ONVIF events (not authorized), giving up until the next restart");
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    // stopping
                }

                if (subscription != null && token.IsCancellationRequested)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var onvif = _onvif;
                        if (onvif != null)
                        {
                            await onvif.UnsubscribeAsync(subscription, cts.Token);
                        }
                    }
                    catch (Exception)
                    {
                        // it expires on its own
                    }
                    return;
                }

                try
                {
                    await Task.Delay(_driver.EventRetryDelay * Math.Min(failures, 10), token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Motion: any topic containing "Motion" (CellMotionDetector, MotionAlarm, ...).
        /// Person: topics or items naming people / persons / humans (e.g. Reolink PeopleDetect, IsPeople).
        /// A state stays set until the camera reports false for the same topic and item.
        /// </summary>
        internal void ApplyEvents(IEnumerable<OnvifEvent> events)
        {
            bool? motion = null, person = null;
            lock (_motionStates)
            {
                foreach (var e in events)
                {
                    var key = $"{e.Topic}|{e.Name}";
                    if (IsPerson(e))
                    {
                        _personStates[key] = e.Value;
                        person = _personStates.Values.Any(v => v);
                    }
                    else if (e.Topic.Contains("Motion", StringComparison.OrdinalIgnoreCase) || e.Name.Equals("IsMotion", StringComparison.OrdinalIgnoreCase))
                    {
                        _motionStates[key] = e.Value;
                        motion = _motionStates.Values.Any(v => v);
                    }
                }
            }

            if (motion.HasValue)
            {
                foreach (var node in _motion)
                {
                    node.Set(motion.Value);
                }
            }
            if (person.HasValue)
            {
                foreach (var node in _person)
                {
                    node.Set(person.Value);
                }
            }
        }

        private static bool IsPerson(OnvifEvent e)
        {
            static bool Names(string s) => s.Contains("People", StringComparison.OrdinalIgnoreCase) ||
                                           s.Contains("Person", StringComparison.OrdinalIgnoreCase) ||
                                           s.Contains("Human", StringComparison.OrdinalIgnoreCase);
            return Names(e.Topic) || Names(e.Name);
        }

        #endregion

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _driver.PollAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = new ValueNode(ctx);
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "online":
                    _online.Add(node);
                    return node;
                case "motion":
                    _motion.Add(node);
                    return node;
                case "person":
                    _person.Add(node);
                    return node;
            }
            return null;
        }

        private static string Blank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static CancellationTokenSource Timeout(CancellationToken token, TimeSpan timeout)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);
            return cts;
        }
    }
}
