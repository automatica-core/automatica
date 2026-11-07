using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.WebApi.Camera;
using Automatica.Core.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// go2rtc stand-in: known streams, JPEG frames per stream, and a WebSocket that answers the MSE request like go2rtc
    /// </summary>
    public sealed class FakeGo2Rtc : Go2RtcProxy
    {
        public FakeGo2Rtc() : base(new Go2RtcSettings(new ConfigurationBuilder().Build()))
        {
        }

        public HashSet<string> Streams { get; } = new();
        public Dictionary<string, byte[]> Frames { get; } = new();
        public bool Down { get; set; }
        public ConcurrentQueue<string> Connected { get; } = new();
        public ConcurrentQueue<string> FrameRequests { get; } = new();
        public TaskCompletionSource UpstreamClosed { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<IReadOnlySet<string>> GetStreamNamesAsync(CancellationToken token)
        {
            if (Down)
            {
                throw new HttpRequestException("connection refused");
            }
            return Task.FromResult<IReadOnlySet<string>>(Streams.ToHashSet());
        }

        public override Task<byte[]> GetFrameAsync(string stream, CancellationToken token)
        {
            FrameRequests.Enqueue(stream);
            return Task.FromResult(Frames.TryGetValue(stream, out var frame) ? frame : null);
        }

        public override async Task<WebSocket> ConnectAsync(string stream, CancellationToken token)
        {
            Connected.Enqueue(stream);
            UpstreamClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // a connected loopback socket pair: one end is the relay's upstream, the other plays go2rtc
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var client = new TcpClient();
            var connect = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port, token);
            var server = await listener.AcceptTcpClientAsync(token);
            await connect;

            var upstream = WebSocket.CreateFromStream(client.GetStream(), false, null, TimeSpan.FromSeconds(30));
            var go2rtc = WebSocket.CreateFromStream(server.GetStream(), true, null, TimeSpan.FromSeconds(30));
            _ = Task.Run(() => ServeAsync(go2rtc, stream, server, client));
            return upstream;
        }

        private async Task ServeAsync(WebSocket socket, string stream, TcpClient server, TcpClient client)
        {
            var buffer = new byte[4096];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (text.Contains("\"mse\""))
                    {
                        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"mse\",\"value\":\"video/mp4; codecs=\\\"avc1.64001F\\\"\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
                        await socket.SendAsync(Encoding.UTF8.GetBytes("init:" + stream), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception)
            {
                // relay gone
            }
            finally
            {
                UpstreamClosed.TrySetResult();
                socket.Dispose();
                server.Dispose();
                client.Dispose();
            }
        }
    }

    public sealed class CameraFixture : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, NodeInstance> _nodes = new();

        public WebApiHost Host { get; }
        public FakeGo2Rtc Go2Rtc { get; } = new();
        public Guid OwnGroup { get; }
        public Guid OtherGroup { get; }
        public Guid Camera { get; }
        public Guid CameraOnline { get; }
        public Guid OtherCamera { get; }
        public Guid PlainNode { get; }

        public CameraFixture()
        {
            var nodeCache = new Mock<INodeInstanceCache>();
            nodeCache.Setup(a => a.Get(It.IsAny<Guid>())).Returns((Guid id) => _nodes.TryGetValue(id, out var node) ? node : null);

            Host = new WebApiHost(configureServices: services =>
            {
                services.RemoveAll<INodeInstanceCache>();
                services.AddSingleton(nodeCache.Object);
                services.RemoveAll<Go2RtcProxy>();
                services.AddSingleton<Go2RtcProxy>(Go2Rtc);
            });

            OwnGroup = Host.CreateUserGroup("cam-own");
            OtherGroup = Host.CreateUserGroup("cam-other");
            Camera = Add("camera", OwnGroup, null);
            CameraOnline = Add("online", null, Camera);
            OtherCamera = Add("camera", OtherGroup, null);
            PlainNode = Add("value", null, null);
        }

        private Guid Add(string templateKey, Guid? group, Guid? parent)
        {
            var id = Guid.NewGuid();
            _nodes[id] = new NodeInstance
            {
                ObjId = id,
                Name = $"{templateKey}-{id}",
                This2UserGroup = group,
                This2ParentNodeInstance = parent,
                This2NodeTemplateNavigation = new NodeTemplate { ObjId = Guid.NewGuid(), Key = templateKey }
            };
            return id;
        }

        public void Dispose() => Host.Dispose();
    }

    /// <summary>
    /// webapi/camera: relays go2rtc to the visu with the normal authentication and the user group visibility
    /// </summary>
    public class CameraHttpTests : IClassFixture<CameraFixture>
    {
        private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3 };

        private readonly CameraFixture _fixture;
        private readonly WebApiHost _host;
        private readonly FakeGo2Rtc _go2Rtc;

        public CameraHttpTests(CameraFixture fixture)
        {
            _fixture = fixture;
            _host = fixture.Host;
            _go2Rtc = fixture.Go2Rtc;
            _go2Rtc.Down = false;
            _go2Rtc.Streams.Clear();
            _go2Rtc.Frames.Clear();
            _go2Rtc.FrameRequests.Clear();
            _go2Rtc.Connected.Clear();
        }

        private string Main(Guid id) => Go2RtcProxy.MainStream(id);
        private string Sub(Guid id) => Go2RtcProxy.SubStream(id);
        private string Snap(Guid id) => Go2RtcProxy.SnapshotStream(id);

        private HttpClient Visu() => _host.CreateClient(_host.CreateVisuToken(_fixture.OwnGroup));

        [Fact]
        public async Task Snapshot_PrefersTheCameraSnapshot()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.UnionWith(new[] { Main(cam), Sub(cam), Snap(cam) });
            _go2Rtc.Frames[Snap(cam)] = Jpeg;

            var response = await Visu().GetAsync($"webapi/camera/{cam}/snapshot");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Jpeg, await response.Content.ReadAsByteArrayAsync());
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
            Assert.Equal(new[] { Snap(cam) }, _go2Rtc.FrameRequests);
        }

        [Fact]
        public async Task Snapshot_NoSnapshotStream_UsesAFrameOfTheSubStream()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.UnionWith(new[] { Main(cam), Sub(cam) });
            _go2Rtc.Frames[Sub(cam)] = Jpeg;

            var response = await Visu().GetAsync($"webapi/camera/{cam}/snapshot");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new[] { Sub(cam) }, _go2Rtc.FrameRequests);
        }

        [Fact]
        public async Task Snapshot_SnapshotFails_FallsBackToTheStream_AndReports502WhenNothingWorks()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.UnionWith(new[] { Main(cam), Snap(cam) });

            var response = await Visu().GetAsync($"webapi/camera/{cam}/snapshot?stream=main");

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Equal(new[] { Snap(cam), Main(cam) }, _go2Rtc.FrameRequests);
        }

        [Fact]
        public async Task Snapshot_ViaADataPointOfTheCamera_ResolvesTheCamera()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.Add(Main(cam));
            _go2Rtc.Frames[Main(cam)] = Jpeg;

            var response = await Visu().GetAsync($"webapi/camera/{_fixture.CameraOnline}/snapshot");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Camera_OfOtherGroupOrUnknown_LooksUnknownToVisu_ButNotToAdmin()
        {
            var other = _fixture.OtherCamera;
            _go2Rtc.Streams.Add(Main(other));
            _go2Rtc.Frames[Main(other)] = Jpeg;

            Assert.Equal(HttpStatusCode.NotFound, (await Visu().GetAsync($"webapi/camera/{other}/snapshot")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Visu().GetAsync($"webapi/camera/{other}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Visu().GetAsync($"webapi/camera/{Guid.NewGuid()}/snapshot")).StatusCode);
            Assert.Empty(_go2Rtc.FrameRequests);

            var admin = _host.CreateClient(_host.CreateAdminToken());
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"webapi/camera/{other}/snapshot")).StatusCode);
        }

        [Fact]
        public async Task Camera_WithoutToken_Unauthorized()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient().GetAsync($"webapi/camera/{_fixture.Camera}/snapshot")).StatusCode);
        }

        [Fact]
        public async Task Camera_TokenInTheQueryString_IsOnlyAcceptedForTheWebSocket()
        {
            var token = _host.CreateVisuToken(_fixture.OwnGroup);

            var response = await _host.CreateClient().GetAsync($"webapi/camera/{_fixture.Camera}/snapshot?access_token={token}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Info_ListsTheStreamsOfTheCamera()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.UnionWith(new[] { Main(cam), Snap(cam), Sub(Guid.NewGuid()) });

            var json = (JObject)await (await Visu().GetAsync($"webapi/camera/{cam}")).ReadJson();

            Assert.True((bool)json["main"]);
            Assert.False((bool)json["sub"]);
            Assert.True((bool)json["snapshot"]);
            Assert.Equal(cam, (Guid)json["id"]);
        }

        [Fact]
        public async Task Go2RtcDown_502()
        {
            _go2Rtc.Down = true;

            Assert.Equal(HttpStatusCode.BadGateway, (await Visu().GetAsync($"webapi/camera/{_fixture.Camera}/snapshot")).StatusCode);
            Assert.Equal(HttpStatusCode.BadGateway, (await Visu().GetAsync($"webapi/camera/{_fixture.Camera}")).StatusCode);
        }

        [Fact]
        public async Task Live_NotAWebSocketRequest_400()
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await Visu().GetAsync($"webapi/camera/{_fixture.Camera}/ws")).StatusCode);
        }

        private async Task<WebSocket> ConnectAsync(Guid camera, string query, string token)
        {
            var client = _host.Server.CreateWebSocketClient();
            var uri = new Uri(_host.Server.BaseAddress, $"webapi/camera/{camera}/ws?{query}{(token != null ? "&access_token=" + token : "")}");
            return await client.ConnectAsync(new UriBuilder(uri) { Scheme = "ws" }.Uri, CancellationToken.None);
        }

        private static async Task<(WebSocketMessageType Type, string Text)> ReceiveAsync(WebSocket socket)
        {
            var buffer = new byte[4096];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var result = await socket.ReceiveAsync(buffer, cts.Token);
            return (result.MessageType, Encoding.UTF8.GetString(buffer, 0, result.Count));
        }

        [Fact]
        public async Task Live_RelaysTheGo2RtcProtocol_OfTheSubStream()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.UnionWith(new[] { Main(cam), Sub(cam) });

            using var socket = await ConnectAsync(cam, "stream=sub", _host.CreateVisuToken(_fixture.OwnGroup));
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"mse\",\"value\":\"avc1.640029\"}"), WebSocketMessageType.Text, true, CancellationToken.None);

            var reply = await ReceiveAsync(socket);
            var data = await ReceiveAsync(socket);

            Assert.Equal(WebSocketMessageType.Text, reply.Type);
            Assert.Contains("\"mse\"", reply.Text);
            Assert.Equal((WebSocketMessageType.Binary, "init:" + Sub(cam)), data);
            Assert.Equal(new[] { Sub(cam) }, _go2Rtc.Connected);

            // closing the browser side closes the go2rtc side
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            await _go2Rtc.UpstreamClosed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Live_Main_UsesTheMainStream_AndSubFallsBackToMain()
        {
            var cam = _fixture.Camera;
            _go2Rtc.Streams.Add(Main(cam));

            using (await ConnectAsync(cam, "stream=main", _host.CreateVisuToken(_fixture.OwnGroup)))
            {
            }
            using (await ConnectAsync(cam, "stream=sub", _host.CreateVisuToken(_fixture.OwnGroup)))
            {
            }

            Assert.Equal(new[] { Main(cam), Main(cam) }, _go2Rtc.Connected);
        }

        [Fact]
        public async Task Live_WithoutToken_IsRejected()
        {
            _go2Rtc.Streams.Add(Main(_fixture.Camera));

            await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(_fixture.Camera, "stream=sub", null));
            Assert.Empty(_go2Rtc.Connected);
        }

        [Fact]
        public async Task Live_OtherGroup_IsRejected()
        {
            _go2Rtc.Streams.Add(Main(_fixture.OtherCamera));

            await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(_fixture.OtherCamera, "stream=sub", _host.CreateVisuToken(_fixture.OwnGroup)));
            Assert.Empty(_go2Rtc.Connected);
        }
    }

    public class CameraUnitTests
    {
        private static readonly Guid Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

        [Fact]
        public void PickStream_SubPreferredForTiles_MainOnRequest()
        {
            var both = new HashSet<string> { Go2RtcProxy.MainStream(Id), Go2RtcProxy.SubStream(Id) };

            Assert.Equal(Go2RtcProxy.SubStream(Id), CameraController.PickStream(both, Id, null));
            Assert.Equal(Go2RtcProxy.SubStream(Id), CameraController.PickStream(both, Id, "sub"));
            Assert.Equal(Go2RtcProxy.MainStream(Id), CameraController.PickStream(both, Id, "MAIN"));
        }

        [Fact]
        public void PickStream_FallsBackToWhatExists()
        {
            Assert.Equal(Go2RtcProxy.MainStream(Id), CameraController.PickStream(new HashSet<string> { Go2RtcProxy.MainStream(Id) }, Id, "sub"));
            Assert.Equal(Go2RtcProxy.SubStream(Id), CameraController.PickStream(new HashSet<string> { Go2RtcProxy.SubStream(Id) }, Id, "main"));
            Assert.Null(CameraController.PickStream(new HashSet<string> { Go2RtcProxy.SnapshotStream(Id) }, Id, "main"));
        }

        [Fact]
        public void StreamNames_MatchTheDriver()
        {
            // P3.Driver.Camera.Driver.CameraStreams uses the same names
            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e", Go2RtcProxy.MainStream(Id));
            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e_sub", Go2RtcProxy.SubStream(Id));
            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e_snap", Go2RtcProxy.SnapshotStream(Id));
        }

        [Theory]
        [InlineData("/webapi/camera/x/ws", "websocket", true)]
        [InlineData("/webapi/camera/x/ws", "WebSocket", true)]
        [InlineData("/webapi/camera/x/snapshot", "", false)]
        [InlineData("/webapi/nodeInstances", "websocket", false)]
        public void IsCameraWebSocket(string path, string upgrade, bool expected)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            if (!string.IsNullOrEmpty(upgrade))
            {
                context.Request.Headers.Upgrade = upgrade;
            }

            Assert.Equal(expected, CameraController.IsCameraWebSocket(context.Request));
        }

        [Fact]
        public void Settings_DefaultIsTheManagedLocalGo2Rtc_UrlMakesItExternal()
        {
            var managed = new Go2RtcSettings(new ConfigurationBuilder().Build());
            Assert.False(managed.IsExternal);
            Assert.Equal(new Uri("http://127.0.0.1:1984/"), managed.BaseUri);

            var external = new Go2RtcSettings(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["server:go2rtc_url"] = "http://go2rtc:1984/base",
                ["server:go2rtc_username"] = "u"
            }).Build());
            Assert.True(external.IsExternal);
            Assert.Equal(new Uri("http://go2rtc:1984/base/"), external.BaseUri);
            Assert.Equal("u", external.Username);
        }

        [Fact]
        public void ConfigYaml_BindsTheApiToLoopbackAndTurnsTheOtherListenersOff()
        {
            var yaml = Go2RtcProcessHost.ConfigYaml(1984);

            Assert.Contains("listen: \"127.0.0.1:1984\"", yaml);
            Assert.Equal(4, yaml.Split("listen: \"\"").Length - 1);
            Assert.DoesNotContain("streams", yaml);
        }

        [Fact]
        public void FindBinary_ConfiguredPathWins_ElseNextToTheServer()
        {
            var name = OperatingSystem.IsWindows() ? "go2rtc.exe" : "go2rtc";
            var nextToServer = Path.Combine(AppContext.BaseDirectory, name);

            Assert.Equal("/opt/go2rtc", Go2RtcProcessHost.FindBinary("/opt/go2rtc", p => p == "/opt/go2rtc"));
            Assert.Null(Go2RtcProcessHost.FindBinary("/opt/missing", _ => false));
            Assert.Equal(nextToServer, Go2RtcProcessHost.FindBinary(null, p => p == nextToServer || p.EndsWith(name)));
            Assert.Null(Go2RtcProcessHost.FindBinary("", _ => false));
        }

        [Fact]
        public async Task Proxy_Frame_OnlyJpegCounts()
        {
            var handler = new StubHandler(request => request.RequestUri!.Query.Contains("good")
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2 }) { Headers = { ContentType = new("image/jpeg") } } }
                : new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("exec: ffmpeg: not found") });
            var proxy = new Go2RtcProxy(new Go2RtcSettings(new ConfigurationBuilder().Build()), new HttpClient(handler));

            Assert.Equal(new byte[] { 1, 2 }, await proxy.GetFrameAsync("good", CancellationToken.None));
            Assert.Null(await proxy.GetFrameAsync("bad", CancellationToken.None));
            Assert.Equal("http://127.0.0.1:1984/api/frame.jpeg?src=good", handler.Requests[0].ToString());
        }

        [Fact]
        public async Task Proxy_StreamNames_WithBasicAuth()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"a\":{},\"a_sub\":{}}") });
            var settings = new Go2RtcSettings(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["server:go2rtc_url"] = "http://go2rtc:1984",
                ["server:go2rtc_username"] = "admin",
                ["server:go2rtc_password"] = "pw"
            }).Build());
            var proxy = new Go2RtcProxy(settings, new HttpClient(handler));

            var names = await proxy.GetStreamNamesAsync(CancellationToken.None);

            Assert.Equal(new[] { "a", "a_sub" }, names.OrderBy(a => a));
            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:pw")), handler.Authorization[0]);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

            public List<Uri> Requests { get; } = new();
            public List<string> Authorization { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri);
                Authorization.Add(request.Headers.Authorization?.ToString());
                return Task.FromResult(_respond(request));
            }
        }
    }
}
