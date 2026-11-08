using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Camera.Driver;
using P3.Driver.Camera.Driver.Onvif;
using Xunit;

namespace P3.Driver.Camera.Tests
{
    public class CameraDriverTests : DriverFactoryTestBase<CameraDriverFactory>
    {
        private sealed class FakeGo2Rtc
        {
            public HashSet<string> Streams { get; } = new();
            public Dictionary<string, string> Sources { get; } = new();
            public bool Down { get; set; }

            public void Route(FakeHttp http)
            {
                http.On(HttpMethod.Get, "/api/streams", (_, _) =>
                {
                    if (Down)
                    {
                        throw new HttpRequestException("connection refused");
                    }
                    return FakeHttp.Json("{" + string.Join(",", Streams.Select(s => $"\"{s}\":{{\"producers\":[],\"consumers\":[]}}")) + "}");
                });
                http.On(HttpMethod.Delete, "/api/streams", (r, _) =>
                {
                    var name = System.Web.HttpUtility.ParseQueryString(r.RequestUri!.Query)["src"];
                    Streams.Remove(name);
                    Sources.Remove(name);
                    return FakeHttp.Ok();
                });
                http.On(HttpMethod.Put, "/api/streams", (r, _) =>
                {
                    var query = System.Web.HttpUtility.ParseQueryString(r.RequestUri!.Query);
                    Streams.Add(query["name"]);
                    Sources[query["name"]] = query["src"];
                    return FakeHttp.Ok();
                });
            }
        }

        private sealed class FakeDiscovery : IOnvifDiscovery
        {
            public List<DiscoveredCamera> Cameras { get; } = new();

            public Task<IReadOnlyList<DiscoveredCamera>> ProbeAsync(TimeSpan duration, CancellationToken token = default) =>
                Task.FromResult<IReadOnlyList<DiscoveredCamera>>(Cameras.ToList());
        }

        private sealed class Setup
        {
            public CameraDriver Driver { get; init; }
            public FakeHttp Http { get; init; }
            public FakeGo2Rtc Go2Rtc { get; init; }
            public FakeDiscovery Discovery { get; init; }
            public List<(string Host, int Port)> Probes { get; } = new();
            public bool Reachable { get; set; } = true;
        }

        private sealed class CameraNodes
        {
            public NodeInstance Camera { get; init; }
            public NodeInstance Online { get; init; }
            public NodeInstance Motion { get; init; }
            public NodeInstance Person { get; init; }
        }

        private NodeInstance CreateRoot()
        {
            var root = CreateNodeInstance(DriverFactory.DriverGuid);
            root.SetProperty(CameraDriverFactory.Go2RtcUrlKey, "http://go2rtc:1984");
            root.SetProperty(CameraDriverFactory.PollKey, 600);
            return root;
        }

        private CameraNodes AddCamera(NodeInstance root, string host = "192.168.1.20", string main = null, string sub = null, string snapshot = null,
            bool onvif = false, string user = "admin", string password = "s3cr:t")
        {
            var camera = CreateNodeInstance(CameraDriverFactory.CameraTemplate);
            camera.SetProperty(CameraDriverFactory.HostKey, host);
            camera.SetProperty(CameraDriverFactory.OnvifPortKey, 80);
            camera.SetProperty(CameraDriverFactory.UsernameKey, user);
            camera.SetProperty(CameraDriverFactory.PasswordKey, password);
            camera.SetProperty(CameraDriverFactory.UseOnvifKey, onvif);
            camera.SetProperty(CameraDriverFactory.EventsKey, onvif);
            camera.SetProperty(CameraDriverFactory.StreamMainKey, main ?? "");
            camera.SetProperty(CameraDriverFactory.StreamSubKey, sub ?? "");
            camera.SetProperty(CameraDriverFactory.SnapshotUrlKey, snapshot ?? "");
            root.InverseThis2ParentNodeInstanceNavigation.Add(camera);

            NodeInstance Child(Guid template)
            {
                var node = CreateNodeInstance(template);
                camera.InverseThis2ParentNodeInstanceNavigation.Add(node);
                return node;
            }

            return new CameraNodes
            {
                Camera = camera,
                Online = Child(CameraDriverFactory.OnlineTemplate),
                Motion = Child(CameraDriverFactory.MotionTemplate),
                Person = Child(CameraDriverFactory.PersonTemplate)
            };
        }

        private async Task<Setup> StartAsync(NodeInstance root, Action<FakeHttp> routes = null)
        {
            var driver = (CameraDriver)await CreateDriver(root);
            var http = new FakeHttp();
            var go2Rtc = new FakeGo2Rtc();
            routes?.Invoke(http);
            go2Rtc.Route(http);
            var discovery = new FakeDiscovery();
            var setup = new Setup { Driver = driver, Http = http, Go2Rtc = go2Rtc, Discovery = discovery };

            driver.Http = http.Client();
            driver.Discovery = discovery;
            driver.OnlineProbe = (host, port, _) =>
            {
                setup.Probes.Add((host, port));
                return Task.FromResult(setup.Reachable);
            };
            driver.EventRetryDelay = TimeSpan.FromMilliseconds(1);
            await driver.Init();
            foreach (var camera in driver.Cameras)
            {
                await camera.Init();
            }
            return setup;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        [Fact]
        public async Task Poll_ConfiguredUrls_RegistersMainSubAndSnapshotWithCredentials()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://192.168.1.20:554/h264Preview_01_main", sub: "rtsp://192.168.1.20:554/h264Preview_01_sub",
                snapshot: "http://192.168.1.20/cgi-bin/api.cgi?cmd=Snap&channel=0");
            var setup = await StartAsync(root);

            await setup.Driver.PollAsync();

            var id = cam.Camera.ObjId.ToString("D");
            Assert.Equal("rtsp://admin:s3cr%3At@192.168.1.20:554/h264Preview_01_main#backchannel=0", setup.Go2Rtc.Sources[id]);
            Assert.Equal("rtsp://admin:s3cr%3At@192.168.1.20:554/h264Preview_01_sub#backchannel=0", setup.Go2Rtc.Sources[id + "_sub"]);
            Assert.Equal("http://admin:s3cr%3At@192.168.1.20/cgi-bin/api.cgi?cmd=Snap&channel=0", setup.Go2Rtc.Sources[id + "_snap"]);
            Assert.Equal(true, Value(cam.Online));
            Assert.Equal(("192.168.1.20", 554), setup.Probes.Single());
        }

        [Fact]
        public async Task Poll_Again_DoesNotReRegister()
        {
            var root = CreateRoot();
            AddCamera(root, main: "rtsp://cam/main");
            var setup = await StartAsync(root);

            await setup.Driver.PollAsync();
            await setup.Driver.PollAsync();

            Assert.Single(setup.Http.Requests, r => r.Method == HttpMethod.Put);
        }

        [Fact]
        public async Task Poll_Go2RtcRestarted_RegistersTheStreamsAgain()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/main");
            var setup = await StartAsync(root);
            await setup.Driver.PollAsync();

            setup.Go2Rtc.Streams.Clear();
            setup.Go2Rtc.Sources.Clear();
            await setup.Driver.PollAsync();

            Assert.Equal(2, setup.Http.Requests.Count(r => r.Method == HttpMethod.Put));
            Assert.Contains(cam.Camera.ObjId.ToString("D"), setup.Go2Rtc.Streams);
        }

        [Fact]
        public async Task Poll_StaleStreamOfAnOlderRun_IsReplaced()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/new");
            var setup = await StartAsync(root);
            var id = cam.Camera.ObjId.ToString("D");
            setup.Go2Rtc.Streams.Add(id);
            setup.Go2Rtc.Sources[id] = "rtsp://cam/old";

            await setup.Driver.PollAsync();

            Assert.Equal("rtsp://admin:s3cr%3At@cam/new#backchannel=0", setup.Go2Rtc.Sources[id]);
        }

        [Fact]
        public async Task Poll_Go2RtcDown_KeepsPollingAndRegistersWhenItIsBack()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/main");
            var setup = await StartAsync(root);
            setup.Go2Rtc.Down = true;
            setup.Reachable = false;

            await setup.Driver.PollAsync();

            Assert.Empty(setup.Go2Rtc.Streams);
            Assert.Equal(false, Value(cam.Online));

            setup.Go2Rtc.Down = false;
            await setup.Driver.PollAsync();

            Assert.Single(setup.Go2Rtc.Streams);
        }

        [Fact]
        public async Task Poll_SubEqualsMain_NoSubStream()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/main", sub: "rtsp://cam/main", user: "");
            var setup = await StartAsync(root);

            await setup.Driver.PollAsync();

            Assert.Equal(new[] { cam.Camera.ObjId.ToString("D") }, setup.Go2Rtc.Streams);
            Assert.Equal("rtsp://cam/main#backchannel=0", setup.Go2Rtc.Sources.Values.Single());
        }

        [Fact]
        public async Task Poll_NoUrlAndNoOnvif_RegistersNothingAndProbesTheHost()
        {
            var root = CreateRoot();
            AddCamera(root, host: "cam.local");
            var setup = await StartAsync(root);

            await setup.Driver.PollAsync();

            Assert.Empty(setup.Go2Rtc.Streams);
            Assert.Equal(("cam.local", 554), setup.Probes.Single());
        }

        private static void OnvifRoutes(FakeHttp http)
        {
            http.OnSoap("/onvif/device_service", "GetSystemDateAndTime", OnvifClientTests.SystemTime)
                .OnSoap("/onvif/device_service", "GetCapabilities", OnvifClientTests.Capabilities("192.168.1.20"))
                .OnSoap("/onvif/Media", "GetProfiles", OnvifClientTests.Profiles)
                .On(HttpMethod.Post, "/onvif/Media", (_, body) => body.Contains("GetSnapshotUri")
                    ? FakeHttp.Soap(OnvifClientTests.SnapshotUri("http://192.168.1.20/onvif/snapshot?token=Profile_1"))
                    : FakeHttp.Soap(OnvifClientTests.StreamUri(body.Contains("Profile_1") ? "rtsp://192.168.1.20:554/Streaming/101" : "rtsp://192.168.1.20:554/Streaming/102")));
        }

        [Fact]
        public async Task Poll_Onvif_FillsInTheMissingUrls()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, onvif: true, sub: "rtsp://192.168.1.20:554/custom");
            var setup = await StartAsync(root, OnvifRoutes);

            await setup.Driver.PollAsync();

            var id = cam.Camera.ObjId.ToString("D");
            Assert.Equal("rtsp://admin:s3cr%3At@192.168.1.20:554/Streaming/101#backchannel=0", setup.Go2Rtc.Sources[id]);
            // the configured sub stream wins over ONVIF
            Assert.Equal("rtsp://admin:s3cr%3At@192.168.1.20:554/custom#backchannel=0", setup.Go2Rtc.Sources[id + "_sub"]);
            Assert.Equal("http://admin:s3cr%3At@192.168.1.20/onvif/snapshot?token=Profile_1", setup.Go2Rtc.Sources[id + "_snap"]);
        }

        [Fact]
        public async Task Poll_OnvifFailsWithoutConfiguredUrl_RetriesLater()
        {
            var root = CreateRoot();
            AddCamera(root, onvif: true);
            var setup = await StartAsync(root, http => http.OnSoap("/onvif/device_service", "GetCapabilities", OnvifClientTests.NotAuthorized, System.Net.HttpStatusCode.BadRequest));

            await setup.Driver.PollAsync();
            await setup.Driver.PollAsync();

            Assert.Empty(setup.Go2Rtc.Streams);
            // the second poll is inside the retry minute: no second ONVIF round
            Assert.Single(setup.Http.Requests, r => r.Body != null && r.Body.Contains("GetCapabilities"));
        }

        [Fact]
        public async Task Poll_OnvifFailsWithConfiguredUrl_UsesTheConfiguredUrl()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, onvif: true, main: "rtsp://192.168.1.20/live");
            var setup = await StartAsync(root, http => http.OnSoap("/onvif/device_service", "GetCapabilities", OnvifClientTests.NotAuthorized, System.Net.HttpStatusCode.BadRequest));

            await setup.Driver.PollAsync();

            Assert.Equal("rtsp://admin:s3cr%3At@192.168.1.20/live#backchannel=0", setup.Go2Rtc.Sources[cam.Camera.ObjId.ToString("D")]);
        }

        [Fact]
        public async Task ApplyEvents_MotionAndPerson()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/main");
            var setup = await StartAsync(root);
            var camera = setup.Driver.Cameras.Single();

            camera.ApplyEvents(new[]
            {
                new OnvifEvent("tns1:RuleEngine/CellMotionDetector/Motion", "IsMotion", true),
                new OnvifEvent("tns1:RuleEngine/MyRuleDetector/PeopleDetect", "State", true)
            });
            Assert.Equal(true, Value(cam.Motion));
            Assert.Equal(true, Value(cam.Person));

            // a second motion source keeps the motion up until both report false
            camera.ApplyEvents(new[] { new OnvifEvent("tns1:VideoSource/MotionAlarm", "State", true) });
            camera.ApplyEvents(new[] { new OnvifEvent("tns1:RuleEngine/CellMotionDetector/Motion", "IsMotion", false) });
            Assert.Equal(true, Value(cam.Motion));

            camera.ApplyEvents(new[]
            {
                new OnvifEvent("tns1:VideoSource/MotionAlarm", "State", false),
                new OnvifEvent("tns1:RuleEngine/MyRuleDetector/PeopleDetect", "State", false)
            });
            Assert.Equal(false, Value(cam.Motion));
            Assert.Equal(false, Value(cam.Person));
        }

        [Fact]
        public async Task ApplyEvents_UnrelatedTopic_ChangesNothing()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, main: "rtsp://cam/main");
            var setup = await StartAsync(root);

            setup.Driver.Cameras.Single().ApplyEvents(new[] { new OnvifEvent("tns1:Device/Trigger/DigitalInput", "LogicalState", true) });

            Assert.Null(Value(cam.Motion));
            Assert.Null(Value(cam.Person));
        }

        [Fact]
        public async Task EventLoop_FollowsTheCameraEvents()
        {
            var root = CreateRoot();
            var cam = AddCamera(root, onvif: true, main: "rtsp://192.168.1.20/live");
            var pulls = 0;
            var setup = await StartAsync(root, http =>
            {
                OnvifRoutes(http);
                http.OnSoap("/onvif/Events", "CreatePullPointSubscription",
                    "<tev:CreatePullPointSubscriptionResponse><tev:SubscriptionReference><wsa5:Address>http://192.168.1.20/onvif/Subscription?Idx=1</wsa5:Address>" +
                    "</tev:SubscriptionReference></tev:CreatePullPointSubscriptionResponse>");
                http.On(HttpMethod.Post, "/onvif/Subscription", (_, body) =>
                {
                    if (body.Contains("PullMessages"))
                    {
                        Interlocked.Increment(ref pulls);
                        return FakeHttp.Soap(OnvifClientTests.HikvisionMotion(true));
                    }
                    return FakeHttp.Soap("<wsnt:UnsubscribeResponse/>");
                });
            });
            var camera = setup.Driver.Cameras.Single();
            await camera.Start();

            await setup.Driver.PollAsync();
            for (var i = 0; i < 200 && Value(cam.Motion) == null; i++)
            {
                await Task.Delay(10);
            }

            Assert.True(camera.EventsRunning);
            Assert.Equal(true, Value(cam.Motion));

            await camera.Stop();
            Assert.False(camera.EventsRunning);
            Assert.True(pulls >= 1);
            // stopping removes the streams from go2rtc
            Assert.Empty(setup.Go2Rtc.Streams);
            Assert.Contains(setup.Http.Requests, r => r.Body != null && r.Body.Contains("Unsubscribe"));
        }

        [Fact]
        public async Task Scan_ReturnsNewOnvifCamerasWithTheirValueNodes()
        {
            var root = CreateRoot();
            AddCamera(root, host: "192.168.1.20", main: "rtsp://192.168.1.20/live");
            var setup = await StartAsync(root);
            setup.Discovery.Cameras.Add(new DiscoveredCamera("192.168.1.20", 80, "Front door", "DS-2CD", "http://192.168.1.20/onvif/device_service"));
            setup.Discovery.Cameras.Add(new DiscoveredCamera("192.168.1.21", 8000, "Garden", "RLC-810A", "http://192.168.1.21:8000/onvif/device_service"));

            var result = await setup.Driver.Scan();

            var node = Assert.Single(result);
            Assert.Equal("Garden RLC-810A (192.168.1.21)", node.Name);
            Assert.Equal("192.168.1.21", node.GetPropertyValueString(CameraDriverFactory.HostKey));
            Assert.Equal(8000, node.GetPropertyValueInt(CameraDriverFactory.OnvifPortKey));
            Assert.Equal(CameraDriverFactory.CameraValueTemplates.Length, node.InverseThis2ParentNodeInstanceNavigation.Count);
        }

        [Fact]
        public async Task AddMissingValueNodes_KeepsTheNodesTheCoreAlreadyCreated()
        {
            var setup = await StartAsync(CreateRoot());
            // the core creates the default children together with the camera
            var camera = CreateNodeInstance(CameraDriverFactory.CameraTemplate);
            camera.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(CameraDriverFactory.OnlineTemplate));
            camera.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(CameraDriverFactory.MotionTemplate));

            setup.Driver.AddMissingValueNodes(camera);

            Assert.Equal(CameraDriverFactory.CameraValueTemplates.OrderBy(t => t),
                camera.InverseThis2ParentNodeInstanceNavigation.Select(c => c.This2NodeTemplate!.Value).OrderBy(t => t));
        }

        [Fact]
        public void CameraStreams_NamesMatchTheCoreContract()
        {
            var id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e", CameraStreams.Main(id));
            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e_sub", CameraStreams.Sub(id));
            Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e_snap", CameraStreams.Snapshot(id));
        }
    }
}
