using System;
using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services.Factories
{
    public class GetMediaInfoResponseFactory
    {
        public GetMediaInfoResponse CreateFor(XNamespace actionXNamespace, string xml)
        {
            var xElement = XElement.Parse(xml);

            var responseNode = xElement.Descendants(actionXNamespace + "GetMediaInfoResponse").First();

            return new GetMediaInfoResponse
            {
                NumberOfTracks = Convert.ToInt32(responseNode.Element("NrTracks")?.Value),
                MediaDuration = responseNode.Element("MediaDuration")?.Value,
                CurrentUri = responseNode.Element("CurrentURI")?.Value,
                CurrentUriMetaData = responseNode.Element("CurrentURIMetaData")?.Value,
                NextUri = responseNode.Element("NextURI")?.Value,
                NextUriMetaData = responseNode.Element("NextURIMetaData")?.Value,
                PlayMedium = responseNode.Element("PlayMedium")?.Value,
                RecordMedium = responseNode.Element("RecordMedium")?.Value,
                WriteStatus = responseNode.Element("WriteStatus")?.Value
            };
        }
    }
}