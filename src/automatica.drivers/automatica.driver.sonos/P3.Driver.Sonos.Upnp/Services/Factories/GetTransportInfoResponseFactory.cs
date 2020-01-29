using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services.Factories
{
    public class GetTransportInfoResponseFactory
    {
        public GetTransportInfoResponse CreateFor(XNamespace actionXNamespace, string xml)
        {
            XElement element = XElement.Parse(xml);

            var responseNode = element.Descendants(actionXNamespace + "GetTransportInfoResponse").First();

            return new GetTransportInfoResponse
            {
                CurrentTransportState = new TransportState(responseNode.Element("CurrentTransportState")?.Value),
                CurrentTransportStatus = responseNode.Element("CurrentTransportStatus")?.Value,
                CurrentSpeed = responseNode.Element("CurrentSpeed")?.Value
            };
        }
    }
}
