using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace P3.Driver.Sonos.Upnp
{
    /// <summary>
    /// Parses XML from the network without DTD processing (no entity expansion, no external resources)
    /// </summary>
    public static class SafeXml
    {
        private static XmlReaderSettings CreateSettings()
        {
            return new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = true
            };
        }

        public static XElement ParseElement(string xml)
        {
            using (var text = new StringReader(xml))
            using (var reader = XmlReader.Create(text, CreateSettings()))
            {
                return XElement.Load(reader);
            }
        }

        public static XDocument ParseDocument(string xml)
        {
            using (var text = new StringReader(xml))
            using (var reader = XmlReader.Create(text, CreateSettings()))
            {
                return XDocument.Load(reader);
            }
        }
    }
}
