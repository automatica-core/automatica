using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using P3.Driver.Camera.Driver.Onvif;
using Xunit;

namespace P3.Driver.Camera.Tests
{
    public class OnvifClientTests
    {
        internal const string SystemTime =
            "<tds:GetSystemDateAndTimeResponse><tds:SystemDateAndTime><tt:DateTimeType>NTP</tt:DateTimeType><tt:UTCDateTime>" +
            "<tt:Time><tt:Hour>12</tt:Hour><tt:Minute>0</tt:Minute><tt:Second>30</tt:Second></tt:Time>" +
            "<tt:Date><tt:Year>2026</tt:Year><tt:Month>11</tt:Month><tt:Day>7</tt:Day></tt:Date>" +
            "</tt:UTCDateTime></tds:SystemDateAndTime></tds:GetSystemDateAndTimeResponse>";

        internal static string Capabilities(string host) =>
            "<tds:GetCapabilitiesResponse><tds:Capabilities>" +
            $"<tt:Device><tt:XAddr>http://{host}/onvif/device_service</tt:XAddr></tt:Device>" +
            $"<tt:Events><tt:XAddr>http://{host}/onvif/Events</tt:XAddr><tt:WSPullPointSupport>true</tt:WSPullPointSupport></tt:Events>" +
            $"<tt:Media><tt:XAddr>http://{host}/onvif/Media</tt:XAddr></tt:Media>" +
            "</tds:Capabilities></tds:GetCapabilitiesResponse>";

        internal const string Profiles =
            "<trt:GetProfilesResponse>" +
            "<trt:Profiles token=\"Profile_2\" fixed=\"true\"><tt:Name>sub</tt:Name><tt:VideoEncoderConfiguration token=\"v2\"><tt:Name>v2</tt:Name>" +
            "<tt:Encoding>H264</tt:Encoding><tt:Resolution><tt:Width>640</tt:Width><tt:Height>360</tt:Height></tt:Resolution></tt:VideoEncoderConfiguration></trt:Profiles>" +
            "<trt:Profiles token=\"Profile_1\" fixed=\"true\"><tt:Name>main</tt:Name><tt:VideoEncoderConfiguration token=\"v1\"><tt:Name>v1</tt:Name>" +
            "<tt:Encoding>H264</tt:Encoding><tt:Resolution><tt:Width>2560</tt:Width><tt:Height>1440</tt:Height></tt:Resolution></tt:VideoEncoderConfiguration></trt:Profiles>" +
            "</trt:GetProfilesResponse>";

        internal static string StreamUri(string uri) =>
            $"<trt:GetStreamUriResponse><trt:MediaUri><tt:Uri>{uri}</tt:Uri><tt:InvalidAfterConnect>false</tt:InvalidAfterConnect></trt:MediaUri></trt:GetStreamUriResponse>";

        internal static string SnapshotUri(string uri) =>
            $"<trt:GetSnapshotUriResponse><trt:MediaUri><tt:Uri>{uri}</tt:Uri></trt:MediaUri></trt:GetSnapshotUriResponse>";

        internal const string NotAuthorized =
            "<SOAP-ENV:Fault><SOAP-ENV:Code><SOAP-ENV:Value>SOAP-ENV:Sender</SOAP-ENV:Value><SOAP-ENV:Subcode><SOAP-ENV:Value>ter:NotAuthorized</SOAP-ENV:Value>" +
            "</SOAP-ENV:Subcode></SOAP-ENV:Code><SOAP-ENV:Reason><SOAP-ENV:Text xml:lang=\"en\">Sender not Authorized</SOAP-ENV:Text></SOAP-ENV:Reason></SOAP-ENV:Fault>";

        private static readonly DateTimeOffset LocalNow = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void PasswordDigest_MatchesTheOnvifProgrammersGuideExample()
        {
            var nonce = Convert.FromBase64String("LKqI6G/AikKCQrN0zqZFlg==");

            Assert.Equal("tuOSpGlFlIXsozq4HFNeeGeFLEI=", OnvifClient.PasswordDigest(nonce, "2010-09-16T07:50:45Z", "userpassword"));
        }

        [Fact]
        public void BuildEnvelope_WithUser_HasUsernameTokenWithDigest()
        {
            var nonce = new byte[16];
            var created = new DateTimeOffset(2026, 11, 7, 12, 0, 30, TimeSpan.Zero);
            var doc = OnvifClient.BuildEnvelope(new XElement(OnvifClient.Media + "GetProfiles"), "admin", "secret", created, nonce, null, null);
            var xml = doc.ToString();

            Assert.Equal("admin", doc.Descendants().Single(e => e.Name.LocalName == "Username").Value);
            Assert.Equal(OnvifClient.PasswordDigest(nonce, "2026-11-07T12:00:30.000Z", "secret"), doc.Descendants().Single(e => e.Name.LocalName == "Password").Value);
            Assert.Contains("2026-11-07T12:00:30.000Z", xml);
            Assert.DoesNotContain(">secret<", xml);
        }

        [Fact]
        public void BuildEnvelope_WithoutUser_NoSecurityHeader()
        {
            var doc = OnvifClient.BuildEnvelope(new XElement(OnvifClient.Device + "GetSystemDateAndTime"), null, "x", LocalNow, new byte[16], null, null);

            Assert.DoesNotContain("Security", doc.ToString());
        }

        [Fact]
        public async Task ParseProfiles_ReadsTokensAndResolutions()
        {
            var body = XDocument.Parse(await FakeHttp.Soap(Profiles).Content.ReadAsStringAsync()).Root!;

            var profiles = OnvifClient.ParseProfiles(body);

            Assert.Equal(2, profiles.Count);
            Assert.Equal(new OnvifProfile("Profile_2", "sub", 640, 360, "H264"), profiles[0]);
            Assert.Equal(2560 * 1440, profiles[1].Pixels);
        }

        [Fact]
        public async Task ConnectAndGetStreams_PicksLargestAsMainAndSmallestAsSub()
        {
            var http = new FakeHttp()
                .OnSoap("/onvif/device_service", "GetSystemDateAndTime", SystemTime)
                .OnSoap("/onvif/device_service", "GetCapabilities", Capabilities("192.168.1.20"))
                .OnSoap("/onvif/Media", "GetProfiles", Profiles)
                .On(System.Net.Http.HttpMethod.Post, "/onvif/Media", (_, body) => body.Contains("Profile_1") && body.Contains("GetStreamUri")
                    ? FakeHttp.Soap(StreamUri("rtsp://192.168.1.20:554/main"))
                    : body.Contains("Profile_2") && body.Contains("GetStreamUri")
                        ? FakeHttp.Soap(StreamUri("rtsp://192.168.1.20:554/sub"))
                        : FakeHttp.Soap(SnapshotUri("http://192.168.1.20/snap.jpg")));
            var client = new OnvifClient(http.Client(), "192.168.1.20", 80, "admin", "secret", () => LocalNow);

            await client.ConnectAsync();
            var streams = await client.GetStreamsAsync();

            Assert.Equal(TimeSpan.FromSeconds(30), client.ClockOffset);
            Assert.Equal(new Uri("http://192.168.1.20/onvif/Media"), client.MediaServiceUri);
            Assert.Equal(new Uri("http://192.168.1.20/onvif/Events"), client.EventServiceUri);
            Assert.Equal("rtsp://192.168.1.20:554/main", streams.MainUri);
            Assert.Equal("rtsp://192.168.1.20:554/sub", streams.SubUri);
            Assert.Equal("http://192.168.1.20/snap.jpg", streams.SnapshotUri);

            // the authenticated calls use the camera clock: local time + 30 s
            var profilesRequest = http.Requests.First(r => r.Body.Contains("GetProfiles"));
            Assert.Contains("2026-11-07T12:00:30", profilesRequest.Body);
            Assert.StartsWith("application/soap+xml", profilesRequest.ContentType);
            // the clock is read without credentials
            Assert.DoesNotContain("UsernameToken", http.Requests.First(r => r.Body.Contains("GetSystemDateAndTime")).Body);
        }

        [Fact]
        public async Task Call_Fault_ThrowsNotAuthorized()
        {
            var http = new FakeHttp()
                .OnSoap("/onvif/device_service", "GetSystemDateAndTime", SystemTime)
                .OnSoap("/onvif/device_service", "GetCapabilities", NotAuthorized, HttpStatusCode.BadRequest);
            var client = new OnvifClient(http.Client(), "cam", 8000, "admin", "wrong", () => LocalNow);

            var ex = await Assert.ThrowsAsync<OnvifException>(() => client.ConnectAsync());

            Assert.True(ex.IsNotAuthorized);
            Assert.Contains("Sender not Authorized", ex.Message);
            Assert.Equal(new Uri("http://cam:8000/onvif/device_service"), client.DeviceServiceUri);
        }

        [Fact]
        public async Task Connect_ClockNeedsAuthentication_StillConnects()
        {
            var http = new FakeHttp()
                .OnSoap("/onvif/device_service", "GetSystemDateAndTime", NotAuthorized, HttpStatusCode.BadRequest)
                .OnSoap("/onvif/device_service", "GetCapabilities", Capabilities("cam"));
            var client = new OnvifClient(http.Client(), "cam", 80, "admin", "secret", () => LocalNow);

            await client.ConnectAsync();

            Assert.Equal(TimeSpan.Zero, client.ClockOffset);
            Assert.NotNull(client.MediaServiceUri);
        }

        [Fact]
        public async Task Call_NoSoapResponse_ThrowsOnvifException()
        {
            var http = new FakeHttp().On(System.Net.Http.HttpMethod.Post, "/onvif/device_service",
                (_, _) => new System.Net.Http.HttpResponseMessage(HttpStatusCode.NotFound) { Content = new System.Net.Http.StringContent("<html>nope") });
            var client = new OnvifClient(http.Client(), "cam", 80, "admin", "secret", () => LocalNow);

            await Assert.ThrowsAsync<OnvifException>(() => client.ConnectAsync());
        }

        /// <summary>
        /// HttpClient only says "An error occurred while sending the request", the log needs the reason behind it
        /// </summary>
        [Fact]
        public async Task Call_TransportError_NamesTheInnerReason()
        {
            var http = new FakeHttp
            {
                Throw = new System.Net.Http.HttpRequestException("An error occurred while sending the request.",
                    new System.IO.IOException("The response ended prematurely."))
            };
            var client = new OnvifClient(http.Client(), "cam", 80, "admin", "secret", () => LocalNow);

            var ex = await Assert.ThrowsAsync<OnvifException>(() =>
                client.PullMessagesAsync(new Uri("http://cam:1024/event-0_1024"), TimeSpan.FromSeconds(1), 10));

            Assert.Equal("PullMessages: An error occurred while sending the request. -> The response ended prematurely.", ex.Message);
        }

        [Fact]
        public async Task PullPoint_SubscribesPullsRenewsAndUnsubscribes()
        {
            const string subscriptionAddress = "http://192.168.1.20/onvif/Subscription?Idx=7";
            var http = new FakeHttp()
                .OnSoap("/onvif/device_service", "GetSystemDateAndTime", SystemTime)
                .OnSoap("/onvif/device_service", "GetCapabilities", Capabilities("192.168.1.20"))
                .OnSoap("/onvif/Events", "CreatePullPointSubscription",
                    $"<tev:CreatePullPointSubscriptionResponse><tev:SubscriptionReference><wsa5:Address>{subscriptionAddress}</wsa5:Address></tev:SubscriptionReference>" +
                    "<wsnt:CurrentTime>2026-11-07T12:00:30Z</wsnt:CurrentTime></tev:CreatePullPointSubscriptionResponse>")
                .OnSoap("/onvif/Subscription", "PullMessages", HikvisionMotion(true))
                .OnSoap("/onvif/Subscription", "Renew", "<wsnt:RenewResponse/>")
                .OnSoap("/onvif/Subscription", "Unsubscribe", "<wsnt:UnsubscribeResponse/>");
            var client = new OnvifClient(http.Client(), "192.168.1.20", 80, "admin", "secret", () => LocalNow);
            await client.ConnectAsync();

            var subscription = await client.CreatePullPointSubscriptionAsync(TimeSpan.FromSeconds(120));
            var events = await client.PullMessagesAsync(subscription, TimeSpan.FromSeconds(10), 50);
            await client.RenewAsync(subscription, TimeSpan.FromSeconds(120));
            await client.UnsubscribeAsync(subscription);

            Assert.Equal(new Uri(subscriptionAddress), subscription);
            Assert.Equal(new OnvifEvent("tns1:RuleEngine/CellMotionDetector/Motion", "IsMotion", true), Assert.Single(events));

            var create = http.Requests.First(r => r.Body.Contains("CreatePullPointSubscription"));
            Assert.Contains("PT2M", create.Body);
            var pull = http.Requests.First(r => r.Body.Contains("PullMessages"));
            Assert.Contains("PT10S", pull.Body);
            Assert.Contains("<MessageLimit>50</MessageLimit>", pull.Body.Replace(" xmlns=\"http://www.onvif.org/ver10/events/wsdl\"", ""));
            // the subscription manager wants the WS-Addressing headers
            Assert.Contains(subscriptionAddress.Replace("&", "&amp;"), pull.Body);
            Assert.Contains("PullMessagesRequest", pull.Body);
            Assert.Contains("action=\"http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest\"", pull.ContentType);
            Assert.Equal("/onvif/Subscription?Idx=7", pull.Uri.PathAndQuery);
        }

        [Fact]
        public async Task CreatePullPoint_NoEventService_Throws()
        {
            var http = new FakeHttp()
                .OnSoap("/onvif/device_service", "GetSystemDateAndTime", SystemTime)
                .OnSoap("/onvif/device_service", "GetCapabilities",
                    "<tds:GetCapabilitiesResponse><tds:Capabilities><tt:Media><tt:XAddr>http://cam/onvif/Media</tt:XAddr></tt:Media></tds:Capabilities></tds:GetCapabilitiesResponse>");
            var client = new OnvifClient(http.Client(), "cam", 80, "admin", "secret", () => LocalNow);
            await client.ConnectAsync();

            Assert.Null(client.EventServiceUri);
            await Assert.ThrowsAsync<OnvifException>(() => client.CreatePullPointSubscriptionAsync(TimeSpan.FromMinutes(2)));
        }

        internal static string HikvisionMotion(bool value) =>
            "<tev:PullMessagesResponse><tev:CurrentTime>2026-11-07T12:00:31Z</tev:CurrentTime><tev:TerminationTime>2026-11-07T12:02:31Z</tev:TerminationTime>" +
            "<wsnt:NotificationMessage><wsnt:Topic Dialect=\"http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet\">tns1:RuleEngine/CellMotionDetector/Motion</wsnt:Topic>" +
            "<wsnt:Message><tt:Message UtcTime=\"2026-11-07T12:00:31Z\" PropertyOperation=\"Changed\"><tt:Source>" +
            "<tt:SimpleItem Name=\"VideoSourceConfigurationToken\" Value=\"VideoSourceToken\"/><tt:SimpleItem Name=\"Rule\" Value=\"MyMotionDetectorRule\"/></tt:Source>" +
            $"<tt:Data><tt:SimpleItem Name=\"IsMotion\" Value=\"{value.ToString().ToLowerInvariant()}\"/></tt:Data></tt:Message></wsnt:Message></wsnt:NotificationMessage>" +
            "</tev:PullMessagesResponse>";

        [Fact]
        public async Task ParseNotifications_ReolinkPeopleAndMotionAlarm()
        {
            var xml = await FakeHttp.Soap(
                "<tev:PullMessagesResponse>" +
                "<wsnt:NotificationMessage><wsnt:Topic>tns1:RuleEngine/MyRuleDetector/PeopleDetect</wsnt:Topic><wsnt:Message><tt:Message>" +
                "<tt:Source><tt:SimpleItem Name=\"Source\" Value=\"000\"/></tt:Source><tt:Data><tt:SimpleItem Name=\"State\" Value=\"true\"/></tt:Data></tt:Message></wsnt:Message></wsnt:NotificationMessage>" +
                "<wsnt:NotificationMessage><wsnt:Topic>tns1:VideoSource/MotionAlarm</wsnt:Topic><wsnt:Message><tt:Message>" +
                "<tt:Data><tt:SimpleItem Name=\"State\" Value=\"0\"/><tt:SimpleItem Name=\"Region\" Value=\"left\"/></tt:Data></tt:Message></wsnt:Message></wsnt:NotificationMessage>" +
                "</tev:PullMessagesResponse>").Content.ReadAsStringAsync();

            var events = OnvifClient.ParseNotifications(XDocument.Parse(xml).Root!);

            Assert.Equal(new[]
            {
                new OnvifEvent("tns1:RuleEngine/MyRuleDetector/PeopleDetect", "State", true),
                new OnvifEvent("tns1:VideoSource/MotionAlarm", "State", false)
            }, events);
        }

        [Fact]
        public void ToDuration_IsXmlDuration()
        {
            Assert.Equal("PT10S", OnvifClient.ToDuration(TimeSpan.FromSeconds(10)));
            Assert.Equal("PT2M", OnvifClient.ToDuration(TimeSpan.FromSeconds(120)));
        }
    }
}
