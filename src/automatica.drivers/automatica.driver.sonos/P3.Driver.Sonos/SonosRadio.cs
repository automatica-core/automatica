using System;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos
{
    /// <summary>
    /// What a Sonos player is sent for a radio station: the transport uri and the DIDL metadata (shown as the station name)
    /// </summary>
    public sealed record SonosRadioMedia(string Uri, string Metadata);

    public static class SonosRadio
    {
        /// <summary>
        /// The Sonos service id of radio streams (TuneIn and plain streams) in the metadata
        /// </summary>
        public const string RadioService = "65031";

        private static readonly Regex TuneInUri = new("^x-sonosapi-stream:(s\\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// A station with a TuneIn id plays through TuneIn, otherwise its stream: an http stream as Sonos radio
        /// (x-rincon-mp3radio://), any other url (https, x-rincon-mp3radio://, x-sonosapi-stream:...) as it is.
        /// Null when there is neither an id nor a url.
        /// </summary>
        public static SonosRadioMedia ForStation(string tuneInId, string streamUrl, string title)
        {
            var tuneIn = NormalizeTuneInId(tuneInId);
            if (tuneIn != null)
            {
                return new SonosRadioMedia(string.Format(SonosController.TuneInMediaUrl, tuneIn), TuneInMetadata(tuneIn, title));
            }

            streamUrl = streamUrl?.Trim();
            if (string.IsNullOrEmpty(streamUrl))
            {
                return null;
            }

            var tuneInUri = TuneInUri.Match(streamUrl);
            if (tuneInUri.Success)
            {
                return new SonosRadioMedia(streamUrl, TuneInMetadata(tuneInUri.Groups[1].Value.ToLowerInvariant(), title));
            }

            var uri = streamUrl;
            if (streamUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                uri = "x-rincon-mp3radio://" + streamUrl.Substring("http://".Length);
            }

            return new SonosRadioMedia(uri, StreamMetadata(string.IsNullOrWhiteSpace(title) ? HostOf(streamUrl) : title));
        }

        /// <summary>
        /// "s8007" for "s8007", "S8007" or "8007"; null if it is no TuneIn id
        /// </summary>
        public static string NormalizeTuneInId(string tuneInId)
        {
            var value = tuneInId?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (value.All(char.IsDigit))
            {
                return "s" + value;
            }

            if (value.Length > 1 && (value[0] == 's' || value[0] == 'S') && value.Skip(1).All(char.IsDigit))
            {
                return "s" + value.Substring(1);
            }

            return null;
        }

        internal static string TuneInMetadata(string tuneInId, string title)
        {
            return Didl($"F00092020{tuneInId}", "L", string.IsNullOrWhiteSpace(title) ? "tunein" : title);
        }

        internal static string StreamMetadata(string title)
        {
            return Didl("R:0/0/0", "R:0/0", title);
        }

        private static string Didl(string id, string parentId, string title)
        {
            return "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" " +
                   "xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" +
                   $"<item id=\"{SecurityElement.Escape(id)}\" parentID=\"{parentId}\" restricted=\"true\">" +
                   $"<dc:title>{SecurityElement.Escape(title ?? "")}</dc:title>" +
                   $"<upnp:class>{ItemClass.Stream}</upnp:class>" +
                   $"<desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON{RadioService}_</desc>" +
                   "</item>" +
                   "</DIDL-Lite>";
        }

        private static string HostOf(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host) ? uri.Host : "Radio";
        }
    }
}
