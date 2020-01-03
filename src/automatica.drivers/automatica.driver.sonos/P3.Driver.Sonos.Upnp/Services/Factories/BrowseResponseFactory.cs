using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services.Factories
{
    public class BrowseResponseFactory
    {
        public BrowseResponse CreateFor(string actionXNamespace, string xml)
        {
            var xElement = XDocument.Parse(xml);

            var browseResponseElement = xElement.Descendants(XName.Get("BrowseResponse", actionXNamespace)).First();

            int.TryParse(browseResponseElement.Element("NumberReturned")?.Value, out var numberReturned);
            int.TryParse(browseResponseElement.Element("TotalMatches")?.Value, out var totalMatches);
            int.TryParse(browseResponseElement.Element("UpdateID")?.Value, out var updateId);

            var didlElement = browseResponseElement.Element("Result")?.Value;

            var browseResponse = new BrowseResponse
            {
                NumberReturned = numberReturned,
                TotalMatches = totalMatches,
                UpdateId = updateId,
                DidlRaw = didlElement
            };

            if ((browseResponse.NumberReturned > 0) && (didlElement != null))
            {
                IEnumerable<XElement> itemElements = XElement.Parse(didlElement).Elements();

                foreach (var itemElement in itemElements)
                {
                    browseResponse.Items.Add(CreateItem(itemElement));
                }
            }

            return browseResponse;
        }

        private static Item CreateItem(XElement itemElement)
        {
            XElement resElement = itemElement.FirstNode as XElement;

            TimeSpan.TryParse(resElement?.GetAttributeValueSafe("duration"), out var duration);
            
            return new Item // TODO: TrackMetaData is very similar
            {
                Id = itemElement.GetAttributeValueSafe("id"),
                ParentId = itemElement.GetAttributeValueSafe("parentID"),
                Restricted = itemElement.GetAttributeValueSafe("restricted"),

                Res = resElement?.Value,
                Duration = duration,
                ProtocolInfo = resElement?.GetAttributeValueSafe("protocolInfo"),
                // upnp
                Class = itemElement.GetElementValueSafe(XName.Get("class", SonosNamespaces.UpnpMetadataNamespace)),
                AlbumArtUri = itemElement.GetElementValueSafe(XName.Get("albumArtURI", SonosNamespaces.UpnpMetadataNamespace)),
                Album = itemElement.GetElementValueSafe(XName.Get("album", SonosNamespaces.UpnpMetadataNamespace)),
                // dc
                Creator = itemElement.GetElementValueSafe(XName.Get("creator", SonosNamespaces.PurlDcNamespace)),
                Title = itemElement.GetElementValueSafe(XName.Get("title", SonosNamespaces.PurlDcNamespace)),
                // r
                Ordinal = itemElement.GetElementValueSafe(XName.Get("ordinal", SonosNamespaces.RinConnetWorksNamespace)),
                Type = itemElement.GetElementValueSafe(XName.Get("type", SonosNamespaces.RinConnetWorksNamespace)),
                Description = itemElement.GetElementValueSafe(XName.Get("description", SonosNamespaces.RinConnetWorksNamespace)),
                ResMd = itemElement.GetElementValueSafe(XName.Get("resMD", SonosNamespaces.RinConnetWorksNamespace))
            };
        }
    }
}