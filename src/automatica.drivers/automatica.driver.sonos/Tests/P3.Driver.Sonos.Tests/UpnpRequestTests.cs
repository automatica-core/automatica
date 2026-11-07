using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp;
using P3.Driver.Sonos.Upnp.Proxy;
using P3.Driver.Sonos.Upnp.Proxy.Soap;
using Xunit;

namespace P3.Driver.Sonos.Tests
{
    public class UpnpRequestTests
    {
        private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";

        private static async Task<(string Body, string ContentType, string SoapAction)> Build(string action, string ns, IList<UpnpArgument> args)
        {
            var content = new UpnpRequestContentFactory().CreateFor(new SoapAction(action, ns), new UpnpArgumentList(args));
            var body = await content.ReadAsStringAsync();
            return (body, content.Headers.ContentType!.MediaType!, content.Headers.GetValues("SOAPAction").Single());
        }

        [Fact]
        public void SoapAction_HeaderValue_IsQuotedNamespaceHashAction()
        {
            var action = new SoapAction("Play", "urn:schemas-upnp-org:service:AVTransport:1");

            Assert.Equal("SOAPAction", SoapAction.HeaderName);
            Assert.Equal("\"urn:schemas-upnp-org:service:AVTransport:1#Play\"", action.HeaderValue);
        }

        [Fact]
        public async Task CreateFor_Envelope_HasActionElementInActionNamespace()
        {
            var (body, contentType, soapAction) = await Build("SetVolume", SoapResponses.RenderingNs,
                new List<UpnpArgument> { new("Channel", "Master"), new("DesiredVolume", 42) });

            Assert.Equal("text/xml", contentType);
            Assert.Equal($"\"{SoapResponses.RenderingNs}#SetVolume\"", soapAction);

            var doc = XDocument.Parse(body);
            var actionElement = doc.Root!.Element(SoapNs + "Body")!.Elements().Single();
            Assert.Equal(XName.Get("SetVolume", SoapResponses.RenderingNs), actionElement.Name);

            var args = actionElement.Elements().Select(e => (e.Name.LocalName, e.Value)).ToList();
            Assert.Equal(new[] { ("InstanceID", "0"), ("Channel", "Master"), ("DesiredVolume", "42") }, args);
        }

        [Fact]
        public void ArgumentList_Null_ContainsOnlyInstanceId()
        {
            var list = new UpnpArgumentList(null);

            var arg = Assert.Single(list.Arguments);
            Assert.Equal("InstanceID", arg.Name);
            Assert.Equal(0, arg.Value);
        }

        [Fact]
        public void ArgumentList_ExplicitInstanceId_IsNotDuplicated()
        {
            var list = new UpnpArgumentList(new List<UpnpArgument> { new("CurrentURI", "x"), new("InstanceID", 0) });

            Assert.Single(list.Arguments, a => a.Name == "InstanceID");
            Assert.Equal(2, list.Arguments.Count);
        }

        [Fact]
        public void ArgumentList_InstanceId_IsInsertedFirst()
        {
            var list = new UpnpArgumentList(new List<UpnpArgument> { new("Speed", 1) });

            Assert.Equal("InstanceID", list.Arguments[0].Name);
            Assert.Equal("Speed", list.Arguments[1].Name);
        }

        [Fact]
        public void UpnpArgument_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new UpnpArgument("", 1));
            Assert.Throws<ArgumentException>(() => new UpnpArgument(null!, 1));
        }

        [Fact]
        public void CreateFor_NullArguments_Throw()
        {
            var factory = new UpnpRequestContentFactory();

            Assert.Throws<ArgumentNullException>(() => factory.CreateFor(null!, new UpnpArgumentList()));
            Assert.Throws<ArgumentNullException>(() => factory.CreateFor(new SoapAction("Play", "urn:x"), null!));
        }

        [Fact]
        public async Task CreateFor_TuneInUrl_AmpersandIsEscapedExactlyOnce()
        {
            // regression: the TuneIn url used "&amp;" itself and was escaped a second time ("&amp;amp;")
            var url = string.Format(SonosController.TuneInMediaUrl, "s12345");
            Assert.Equal("x-sonosapi-stream:s12345?sid=254&flags=32", url);

            var (body, _, _) = await Build("SetAVTransportURI", SoapResponses.AvTransportNs,
                new List<UpnpArgument> { new("CurrentURI", url) });

            Assert.Contains("<CurrentURI>x-sonosapi-stream:s12345?sid=254&amp;flags=32</CurrentURI>", body);
            Assert.DoesNotContain("&amp;amp;", body);

            var value = XDocument.Parse(body).Descendants("CurrentURI").Single().Value;
            Assert.Equal(url, value);
        }

        [Fact]
        public async Task CreateFor_MetadataXmlArgument_IsEscapedAndRoundTrips()
        {
            const string metadata = "<DIDL-Lite xmlns=\"urn:x\"><item id=\"F00092020s1\" parentID=\"L\"><dc:title>a & b</dc:title></item></DIDL-Lite>";

            var (body, _, _) = await Build("SetAVTransportURI", SoapResponses.AvTransportNs,
                new List<UpnpArgument> { new("CurrentURIMetaData", metadata) });

            // the argument does not become markup of the SOAP body
            var doc = XDocument.Parse(body);
            var element = doc.Descendants("CurrentURIMetaData").Single();
            Assert.False(element.HasElements);
            Assert.Equal(metadata, element.Value);
        }

        [Fact]
        public async Task CreateFor_InjectionAttempt_DoesNotAddElements()
        {
            var (body, _, _) = await Build("SetMute", SoapResponses.RenderingNs,
                new List<UpnpArgument> { new("Channel", "Master</Channel><DesiredMute>1</DesiredMute><Channel>") });

            var doc = XDocument.Parse(body);
            Assert.Empty(doc.Descendants("DesiredMute"));
            Assert.Single(doc.Descendants("Channel"));
        }

        [Fact]
        public async Task CreateFor_BoolToIntArgument_IsSerializedAsZeroOrOne()
        {
            var (body, _, _) = await Build("SetMute", SoapResponses.RenderingNs,
                new List<UpnpArgument> { new("EnqueueAsNext", true.ToInt()), new("Other", false.ToInt()) });

            var doc = XDocument.Parse(body);
            Assert.Equal("1", doc.Descendants("EnqueueAsNext").Single().Value);
            Assert.Equal("0", doc.Descendants("Other").Single().Value);
        }
    }
}
