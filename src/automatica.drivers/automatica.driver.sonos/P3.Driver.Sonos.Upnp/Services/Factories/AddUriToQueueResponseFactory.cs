using System;
using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services.Factories
{
    public class AddUriToQueueResponseFactory
    {
        public AddUriToQueueResponse CreateFor(XNamespace actionXNamespace, string xml)
        {
            /*
            <?xml version="1.0"?>
            <s:Envelope s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/" xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                <s:Body>
                    <u:AddURIToQueueResponse xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
                        <FirstTrackNumberEnqueued>2</FirstTrackNumberEnqueued>
                        <NumTracksAdded>1</NumTracksAdded>
                        <NewQueueLength>12</NewQueueLength>
                    </u:AddURIToQueueResponse>
                </s:Body>
            </s:Envelope>
            */

            var xElement = XDocument.Parse(xml);

            var responseNode = xElement.Descendants(actionXNamespace + "AddURIToQueueResponse").First();

            return new AddUriToQueueResponse
            {
                FirstTrackNumberEnqueued = Convert.ToInt32(responseNode.Element("FirstTrackNumberEnqueued")?.Value),
                NumTracksAdded = Convert.ToInt32(responseNode.Element("NumTracksAdded")?.Value),
                NewQueueLength = Convert.ToInt32(responseNode.Element("NewQueueLength")?.Value)
            };
        }
    }
}