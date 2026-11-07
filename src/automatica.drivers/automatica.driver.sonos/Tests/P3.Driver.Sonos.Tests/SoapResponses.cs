using System.Security;

namespace P3.Driver.Sonos.Tests
{
    internal static class SoapResponses
    {
        public const string AvTransportNs = "urn:schemas-upnp-org:service:AVTransport:1";
        public const string RenderingNs = "urn:schemas-upnp-org:service:RenderingControl:1";
        public const string ContentDirectoryNs = "urn:schemas-upnp-org:service:ContentDirectory:1";

        /// <summary>an internal entity: harmless by itself, but proves the parser processes DTDs</summary>
        public const string InternalEntityDoctype = "<!DOCTYPE s:Envelope [<!ENTITY xxe \"INJECTED\">]>";

        public static string Envelope(string body, string doctype = "")
        {
            return "<?xml version=\"1.0\"?>" + doctype +
                   "<s:Envelope s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                   "<s:Body>" + body + "</s:Body></s:Envelope>";
        }

        public static string Didl(string items, string doctype = "")
        {
            return doctype + "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" " +
                   "xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                   items + "</DIDL-Lite>";
        }

        public static string TrackItem(string id, string title, string creator, string album, string duration = "0:03:25")
        {
            return $"<item id=\"{id}\" parentID=\"Q:0\" restricted=\"true\">" +
                   $"<res protocolInfo=\"x-file-cifs:*:audio/mpeg:*\" duration=\"{duration}\">x-file-cifs://nas/music/{id}.mp3</res>" +
                   "<upnp:albumArtURI>/getaa?s=1&amp;u=x</upnp:albumArtURI>" +
                   $"<dc:title>{title}</dc:title>" +
                   "<upnp:class>object.item.audioItem.musicTrack</upnp:class>" +
                   $"<dc:creator>{creator}</dc:creator>" +
                   $"<upnp:album>{album}</upnp:album>" +
                   "</item>";
        }

        public static string Escape(string xml) => SecurityElement.Escape(xml);

        public static string PositionInfo(string trackMetaDataEscaped, string relTime = "0:01:10", string trackDuration = "0:03:25", string doctype = "")
        {
            return Envelope($"<u:GetPositionInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                            "<Track>3</Track>" +
                            $"<TrackDuration>{trackDuration}</TrackDuration>" +
                            $"<TrackMetaData>{trackMetaDataEscaped}</TrackMetaData>" +
                            "<TrackURI>x-file-cifs://nas/music/t3.mp3</TrackURI>" +
                            $"<RelTime>{relTime}</RelTime>" +
                            "<AbsTime>NOT_IMPLEMENTED</AbsTime>" +
                            "<RelCount>2147483647</RelCount>" +
                            "<AbsCount>2147483647</AbsCount>" +
                            "</u:GetPositionInfoResponse>", doctype);
        }

        public static string Browse(string didlEscaped, int numberReturned, int totalMatches, int updateId, string doctype = "")
        {
            return Envelope($"<u:BrowseResponse xmlns:u=\"{ContentDirectoryNs}\">" +
                            $"<Result>{didlEscaped}</Result>" +
                            $"<NumberReturned>{numberReturned}</NumberReturned>" +
                            $"<TotalMatches>{totalMatches}</TotalMatches>" +
                            $"<UpdateID>{updateId}</UpdateID>" +
                            "</u:BrowseResponse>", doctype);
        }

        public static string SingleValue(string ns, string action, string element, string value, string doctype = "")
        {
            return Envelope($"<u:{action}Response xmlns:u=\"{ns}\"><{element}>{value}</{element}></u:{action}Response>", doctype);
        }
    }
}
