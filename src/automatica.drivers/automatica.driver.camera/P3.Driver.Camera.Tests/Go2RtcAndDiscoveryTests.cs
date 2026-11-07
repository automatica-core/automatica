using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using P3.Driver.Camera.Driver.Go2Rtc;
using P3.Driver.Camera.Driver.Onvif;
using Xunit;

namespace P3.Driver.Camera.Tests
{
    public class Go2RtcClientTests
    {
        [Fact]
        public async Task SetStream_DeletesThenPutsWithEscapedSource()
        {
            var http = new FakeHttp()
                .On(HttpMethod.Delete, "/api/streams", (_, _) => new HttpResponseMessage(HttpStatusCode.NotFound))
                .On(HttpMethod.Put, "/api/streams", (_, _) => FakeHttp.Ok());
            var client = new Go2RtcClient(http.Client(), "http://go2rtc:1984", null, null);

            await client.SetStreamAsync("cam_sub", "rtsp://admin:p%40ss@cam/s?a=1&b=2#backchannel=0");

            Assert.Equal(new[] { HttpMethod.Delete, HttpMethod.Put }, http.Requests.Select(r => r.Method));
            Assert.Equal("/api/streams?src=cam_sub", http.Requests[0].Uri.PathAndQuery);
            var put = http.Requests[1].Uri;
            var query = System.Web.HttpUtility.ParseQueryString(put.Query);
            Assert.Equal("cam_sub", query["name"]);
            Assert.Equal("rtsp://admin:p%40ss@cam/s?a=1&b=2#backchannel=0", query["src"]);
            Assert.Null(http.Requests[1].Authorization);
        }

        [Fact]
        public async Task SetStream_Rejected_Throws()
        {
            var http = new FakeHttp()
                .On(HttpMethod.Delete, "/api/streams", (_, _) => FakeHttp.Ok())
                .On(HttpMethod.Put, "/api/streams", (_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("unsupported scheme") });
            var client = new Go2RtcClient(http.Client(), "http://go2rtc:1984/", null, null);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SetStreamAsync("cam", "foo://bar"));

            Assert.Contains("unsupported scheme", ex.Message);
        }

        [Fact]
        public async Task GetStreamNames_ParsesTheObjectKeysWithBasicAuth()
        {
            var http = new FakeHttp().On(HttpMethod.Get, "/base/api/streams",
                (_, _) => FakeHttp.Json("{\"a\":{\"producers\":[],\"consumers\":[]},\"b_sub\":{\"producers\":[{\"url\":\"rtsp://x\"}],\"consumers\":[]}}"));
            var client = new Go2RtcClient(http.Client(), "http://go2rtc:1984/base", "admin", "pw");

            var names = await client.GetStreamNamesAsync();

            Assert.Equal(new[] { "a", "b_sub" }, names.OrderBy(n => n));
            Assert.Equal("Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:pw")), http.Requests[0].Authorization);
        }

        [Fact]
        public async Task GetStreamNames_EmptyConfig_Null_IsEmpty()
        {
            var http = new FakeHttp().On(HttpMethod.Get, "/api/streams", (_, _) => FakeHttp.Json("null"));
            var client = new Go2RtcClient(http.Client(), null, null, null);

            Assert.Empty(await client.GetStreamNamesAsync());
            Assert.Equal(new Uri("http://127.0.0.1:1984/api/streams"), http.Requests[0].Uri);
        }
    }

    public class OnvifDiscoveryTests
    {
        [Fact]
        public void ParseProbeMatches_PrefersIpv4AndReadsNameAndHardware()
        {
            const string xml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://www.w3.org/2003/05/soap-envelope\" " +
                "xmlns:wsdd=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:wsa=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\"><SOAP-ENV:Body>" +
                "<wsdd:ProbeMatches><wsdd:ProbeMatch><wsa:EndpointReference><wsa:Address>urn:uuid:1</wsa:Address></wsa:EndpointReference>" +
                "<wsdd:Types>dn:NetworkVideoTransmitter</wsdd:Types>" +
                "<wsdd:Scopes>onvif://www.onvif.org/type/video_encoder onvif://www.onvif.org/name/Garden%20Cam onvif://www.onvif.org/hardware/RLC-810A onvif://www.onvif.org/location/</wsdd:Scopes>" +
                "<wsdd:XAddrs>http://[fe80::1]:8000/onvif/device_service http://192.168.1.21:8000/onvif/device_service</wsdd:XAddrs>" +
                "<wsdd:MetadataVersion>1</wsdd:MetadataVersion></wsdd:ProbeMatch></wsdd:ProbeMatches></SOAP-ENV:Body></SOAP-ENV:Envelope>";

            var camera = Assert.Single(OnvifDiscovery.ParseProbeMatches(xml));

            Assert.Equal("192.168.1.21", camera.Host);
            Assert.Equal(8000, camera.Port);
            Assert.Equal("Garden Cam", camera.Name);
            Assert.Equal("RLC-810A", camera.Hardware);
        }

        [Fact]
        public void ParseProbeMatches_Garbage_Empty()
        {
            Assert.Empty(OnvifDiscovery.ParseProbeMatches("not xml"));
            Assert.Empty(OnvifDiscovery.ParseProbeMatches("<a><ProbeMatch><XAddrs>nope</XAddrs></ProbeMatch></a>"));
        }

        [Fact]
        public void BuildProbe_AsksForVideoTransmitters()
        {
            var probe = OnvifDiscovery.BuildProbe(Guid.Parse("11111111-2222-3333-4444-555555555555"));

            Assert.Contains("dn:NetworkVideoTransmitter", probe);
            Assert.Contains("uuid:11111111-2222-3333-4444-555555555555", probe);
            System.Xml.Linq.XDocument.Parse(probe);
        }
    }
}
