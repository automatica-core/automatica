using System;
using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services.Factories
{
    public class GetPositionInfoResponseFactory
    {
        public GetPositionInfoResponse CreateFor(XNamespace actionXNamespace, string xml)
        {
            XElement element = XElement.Parse(xml);

            var responseNode = element.Descendants(actionXNamespace + "GetPositionInfoResponse").First();

            int trackNumber;
            TimeSpan trackDuration;
            TimeSpan relativeTime;
            int relativeCount;
            int absoluteCount;

            int.TryParse(responseNode.Element("Track")?.Value, out trackNumber);
            TimeSpan.TryParse(responseNode.Element("TrackDuration")?.Value, out trackDuration);
            var trackMetaDataRaw = responseNode.Element("TrackMetaData")?.Value;
            var trackUri = responseNode.Element("TrackURI")?.Value;
            TimeSpan.TryParse(responseNode.Element("RelTime")?.Value, out relativeTime);
            int.TryParse(responseNode.Element("RelCount")?.Value, out relativeCount);
            int.TryParse(responseNode.Element("AbsCount")?.Value, out absoluteCount);

            var trackMetaData = CreateTrackMetaData(trackMetaDataRaw);

            return new GetPositionInfoResponse
            {
                TrackNumber = trackNumber,
                TrackDuration = trackDuration,
                TrackMetaData = trackMetaData,
                TrackMetaDataRaw = trackMetaDataRaw,
                TrackUri = trackUri,
                RelativeTime = relativeTime,
                AbsoluteTime = responseNode.Element("AbsTime")?.Value,
                RelativeCount = relativeCount,
                AbsoluteCount = absoluteCount
            };
        }

        private TrackMetaData CreateTrackMetaData(string trackMetaDataRaw)
        {
            if (IsTv(trackMetaDataRaw))
            {
                return CreateTvTrackMetaData();
            }
            
            XElement didlElement = XElement.Parse(trackMetaDataRaw).FirstNode as XElement;
            XElement resElement = didlElement?.FirstNode as XElement;

            var trackMetaData = new TrackMetaData
            {
                AlbumArtUri = didlElement.GetElementValueSafe(XName.Get("albumArtURI", SonosNamespaces.UpnpMetadataNamespace)),
                Class = didlElement.GetElementValueSafe(XName.Get("class", SonosNamespaces.UpnpMetadataNamespace)),
                Album = didlElement.GetElementValueSafe(XName.Get("album", SonosNamespaces.UpnpMetadataNamespace)),
                Creator = didlElement.GetElementValueSafe(XName.Get("creator", SonosNamespaces.PurlDcNamespace)),
                Title = didlElement.GetElementValueSafe(XName.Get("title", SonosNamespaces.PurlDcNamespace)),
                Duration = resElement?.GetAttributeValueSafe("duration"),
                ProtocolInfo = resElement?.GetAttributeValueSafe("protocolInfo"),
                Res = resElement?.Value
            };

            return trackMetaData;
        }

        private static bool IsTv(string trackMetaDataRaw)
        {
            return string.IsNullOrWhiteSpace(trackMetaDataRaw) || trackMetaDataRaw.Equals("NOT_IMPLEMENTED");
        }

        private static TrackMetaData CreateTvTrackMetaData()
        {
            return new TrackMetaData
            {
                Title = "TV",
                AlbumArtUri = "/Assets/Icons/tv.png"
            };
        }
    }
}
