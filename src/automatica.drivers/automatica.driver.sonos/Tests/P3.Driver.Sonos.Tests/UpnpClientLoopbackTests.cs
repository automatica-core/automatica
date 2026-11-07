using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Proxy;
using P3.Driver.Sonos.Upnp.Services.Factories;
using Xunit;
using static P3.Driver.Sonos.Tests.SoapResponses;

namespace P3.Driver.Sonos.Tests
{
    /// <summary>
    /// The real UpnpClient (HttpClient + SOAP builder + safe parser) against a loopback HTTP server
    /// </summary>
    public class UpnpClientLoopbackTests
    {
        private static Uri ControlUri(LoopbackHttpServer server, string path) => new($"{server.BaseUrl}{path}");

        [Fact]
        public async Task InvokeFuncAsync_GetVolume_PostsSoapAndParsesValue()
        {
            using var server = new LoopbackHttpServer(_ =>
                new LoopbackHttpServer.Response(200, SingleValue(RenderingNs, "GetVolume", "CurrentVolume", "37")));
            var client = new UpnpClient(ControlUri(server, "/MediaRenderer/RenderingControl/Control"), RenderingNs);

            var volume = await client.InvokeFuncAsync<int>("GetVolume", new List<UpnpArgument> { new("Channel", "Master") });

            Assert.Equal(37, volume);
            var request = Assert.Single(server.Requests);
            Assert.Equal("POST", request.Method);
            Assert.Equal("/MediaRenderer/RenderingControl/Control", request.Path);
            Assert.Equal($"\"{RenderingNs}#GetVolume\"", request.Headers["SOAPAction"]);
            Assert.StartsWith("text/xml", request.Headers["Content-Type"]);

            var body = XDocument.Parse(request.Body);
            var action = body.Descendants(XName.Get("GetVolume", RenderingNs)).Single();
            Assert.Equal("0", action.Element("InstanceID")!.Value);
            Assert.Equal("Master", action.Element("Channel")!.Value);
        }

        [Fact]
        public async Task InvokeFuncAsync_StringResult_IsReturned()
        {
            using var server = new LoopbackHttpServer(_ =>
                new LoopbackHttpServer.Response(200, SingleValue(ContentDirectoryNs, "GetLastIndexChange", "LastIndexChange", "S:7")));
            var client = new UpnpClient(ControlUri(server, "/MediaServer/ContentDirectory/Control"), ContentDirectoryNs);

            var value = await client.InvokeFuncAsync<string>("GetLastIndexChange");

            Assert.Equal("S:7", value);
        }

        [Fact]
        public async Task InvokeFuncAsync_PrettyPrintedResponse_StillParsesFirstValue()
        {
            // whitespace text nodes would break the FirstNode walk if whitespace were not ignored
            var xml = "<?xml version=\"1.0\"?>\n<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">\n  <s:Body>\n" +
                      $"    <u:GetMuteResponse xmlns:u=\"{RenderingNs}\">\n      <CurrentMute>1</CurrentMute>\n    </u:GetMuteResponse>\n  </s:Body>\n</s:Envelope>\n";
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, xml));
            var client = new UpnpClient(ControlUri(server, "/c"), RenderingNs);

            Assert.Equal(1, await client.InvokeFuncAsync<int>("GetMute"));
        }

        [Fact]
        public async Task InvokeFuncAsync_DtdInResponse_IsRejected()
        {
            var xml = SingleValue(RenderingNs, "GetVolume", "CurrentVolume", "&xxe;",
                "<!DOCTYPE s:Envelope [<!ENTITY xxe \"99\">]>");
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, xml));
            var client = new UpnpClient(ControlUri(server, "/c"), RenderingNs);

            await Assert.ThrowsAsync<XmlException>(() => client.InvokeFuncAsync<int>("GetVolume"));
        }

        [Fact]
        public async Task InvokeFuncAsync_ExternalDtdInResponse_IsRejectedWithoutFetching()
        {
            using var dtdHost = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, "<!ENTITY xxe \"5\">"));
            var xml = SingleValue(RenderingNs, "GetBass", "CurrentBass", "&xxe;",
                $"<!DOCTYPE s:Envelope SYSTEM \"{dtdHost.BaseUrl}/evil.dtd\">");
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, xml));
            var client = new UpnpClient(ControlUri(server, "/c"), RenderingNs);

            await Assert.ThrowsAsync<XmlException>(() => client.InvokeFuncAsync<int>("GetBass"));
            Assert.Empty(dtdHost.Requests);
        }

        [Fact]
        public async Task InvokeFuncWithResultAsync_ReturnsRawUtf8Body()
        {
            var xml = SingleValue(AvTransportNs, "GetPositionInfo", "TrackURI", "x-file-cifs://nas/Motörhead.mp3");
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, xml));
            var client = new UpnpClient(ControlUri(server, "/MediaRenderer/AVTransport/Control"), AvTransportNs);

            var raw = await client.InvokeFuncWithResultAsync("GetPositionInfo");

            Assert.Equal(xml, raw);
        }

        [Fact]
        public async Task InvokeFuncWithResultAsync_ThroughFactory_ParsesTransportInfo()
        {
            var xml = Envelope($"<u:GetTransportInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<CurrentTransportState>PAUSED_PLAYBACK</CurrentTransportState>" +
                               "<CurrentTransportStatus>OK</CurrentTransportStatus><CurrentSpeed>1</CurrentSpeed>" +
                               "</u:GetTransportInfoResponse>");
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, xml));
            var client = new UpnpClient(ControlUri(server, "/MediaRenderer/AVTransport/Control"), AvTransportNs);

            var raw = await client.InvokeFuncWithResultAsync("GetTransportInfo");
            var info = new GetTransportInfoResponseFactory().CreateFor(AvTransportNs, raw);

            Assert.Equal("PAUSED_PLAYBACK", info.CurrentTransportState.Value);
            Assert.False(info.CurrentTransportState.IsPlaying);
            Assert.Equal($"\"{AvTransportNs}#GetTransportInfo\"", Assert.Single(server.Requests).Headers["SOAPAction"]);
        }

        [Fact]
        public async Task InvokeActionAsync_ReturnsHttpStatus()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(500, "<fault/>"));
            var client = new UpnpClient(ControlUri(server, "/MediaRenderer/AVTransport/Control"), AvTransportNs);

            using var response = await client.InvokeActionAsync("Play", new List<UpnpArgument> { new("Speed", 1) });

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var body = XDocument.Parse(Assert.Single(server.Requests).Body);
            Assert.Equal("1", body.Descendants("Speed").Single().Value);
        }

        [Fact]
        public async Task InvokeActionAsync_WithoutArguments_SendsOnlyInstanceId()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, Envelope("<u:StopResponse xmlns:u=\"x\"/>")));
            var client = new UpnpClient(ControlUri(server, "/c"), AvTransportNs);

            using var response = await client.InvokeActionAsync("Stop");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var action = XDocument.Parse(Assert.Single(server.Requests).Body).Descendants(XName.Get("Stop", AvTransportNs)).Single();
            Assert.Equal(new[] { "InstanceID" }, action.Elements().Select(e => e.Name.LocalName));
        }
    }
}
