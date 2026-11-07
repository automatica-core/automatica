using System;
using System.IO;
using System.Linq;
using System.Xml;
using P3.Driver.Sonos.Upnp;
using Xunit;

namespace P3.Driver.Sonos.Tests
{
    public class SafeXmlTests
    {
        [Fact]
        public void ParseElement_PlainXml_ReturnsRootElement()
        {
            var element = SafeXml.ParseElement("<root><child a=\"1\">value</child></root>");

            Assert.Equal("root", element.Name.LocalName);
            Assert.Equal("value", element.Element("child")!.Value);
            Assert.Equal("1", element.Element("child")!.Attribute("a")!.Value);
        }

        [Fact]
        public void ParseDocument_PlainXml_ReturnsDocument()
        {
            var doc = SafeXml.ParseDocument("<?xml version=\"1.0\"?><root><child>value</child></root>");

            Assert.Equal("root", doc.Root!.Name.LocalName);
            Assert.Equal("value", doc.Root.Element("child")!.Value);
        }

        [Fact]
        public void ParseElement_WhitespaceBetweenElements_IsIgnored()
        {
            var element = SafeXml.ParseElement("<root>\n  <a>1</a>\n  <b>2</b>\n</root>");

            // IgnoreWhitespace: the first node is an element, not a whitespace text node (UpnpClient walks FirstNode)
            Assert.IsType<System.Xml.Linq.XElement>(element.FirstNode);
            Assert.Equal(2, element.Nodes().Count());
        }

        [Fact]
        public void ParseElement_InternalEntityDtd_IsRejected()
        {
            const string xml = "<!DOCTYPE root [<!ENTITY e \"INJECTED\">]><root>&e;</root>";

            Assert.Throws<XmlException>(() => SafeXml.ParseElement(xml));
        }

        [Fact]
        public void ParseDocument_InternalEntityDtd_IsRejected()
        {
            const string xml = "<!DOCTYPE root [<!ENTITY e \"INJECTED\">]><root>&e;</root>";

            Assert.Throws<XmlException>(() => SafeXml.ParseDocument(xml));
        }

        [Fact]
        public void ParseElement_DoctypeWithoutEntities_IsRejected()
        {
            Assert.Throws<XmlException>(() => SafeXml.ParseElement("<!DOCTYPE root><root/>"));
        }

        [Fact]
        public void ParseElement_BillionLaughs_IsRejectedWithoutExpansion()
        {
            var xml = "<?xml version=\"1.0\"?><!DOCTYPE lolz [" +
                      "<!ENTITY lol \"lol\">" +
                      string.Concat(Enumerable.Range(1, 9).Select(i =>
                          $"<!ENTITY lol{i} \"{string.Concat(Enumerable.Repeat(i == 1 ? "&lol;" : $"&lol{i - 1};", 10))}\">")) +
                      "]><lolz>&lol9;</lolz>";

            Assert.Throws<XmlException>(() => SafeXml.ParseElement(xml));
            Assert.Throws<XmlException>(() => SafeXml.ParseDocument(xml));
        }

        [Fact]
        public void ParseElement_ExternalFileEntity_IsRejectedAndFileNotRead()
        {
            var dir = Directory.CreateTempSubdirectory("sonos-xxe-");
            try
            {
                var secret = Path.Combine(dir.FullName, "secret.txt");
                File.WriteAllText(secret, "TOP-SECRET-CONTENT");
                var uri = new Uri(secret).AbsoluteUri;
                var xml = $"<!DOCTYPE root [<!ENTITY xxe SYSTEM \"{uri}\">]><root>&xxe;</root>";

                var ex = Assert.Throws<XmlException>(() => SafeXml.ParseElement(xml));
                Assert.DoesNotContain("TOP-SECRET-CONTENT", ex.Message);
                Assert.Throws<XmlException>(() => SafeXml.ParseDocument(xml));
            }
            finally
            {
                dir.Delete(true);
            }
        }

        [Fact]
        public void ParseDocument_ExternalHttpEntity_DoesNotContactTheHost()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, "<x>leak</x>"));
            var xml = $"<!DOCTYPE root SYSTEM \"{server.BaseUrl}/evil.dtd\"><root>&xxe;</root>";
            var xmlEntity = $"<!DOCTYPE root [<!ENTITY xxe SYSTEM \"{server.BaseUrl}/entity\">]><root>&xxe;</root>";
            var xmlParam = $"<!DOCTYPE root [<!ENTITY % p SYSTEM \"{server.BaseUrl}/param\"> %p;]><root/>";

            Assert.Throws<XmlException>(() => SafeXml.ParseDocument(xml));
            Assert.Throws<XmlException>(() => SafeXml.ParseDocument(xmlEntity));
            Assert.Throws<XmlException>(() => SafeXml.ParseElement(xmlParam));

            // the parser fails synchronously on the DOCTYPE, nothing can have been fetched
            Assert.Empty(server.Requests);
        }

        [Fact]
        public void ParseElement_MalformedXml_Throws()
        {
            Assert.Throws<XmlException>(() => SafeXml.ParseElement("<root><unclosed></root>"));
        }

        [Fact]
        public void ParseElement_EmptyString_Throws()
        {
            Assert.Throws<XmlException>(() => SafeXml.ParseElement(string.Empty));
        }

        [Fact]
        public void ParseElement_PredefinedEntitiesAndCharacterReferences_StillWork()
        {
            var element = SafeXml.ParseElement("<root>a &amp; b &lt;c&gt; &#228;</root>");

            Assert.Equal("a & b <c> ä", element.Value);
        }
    }
}
