using System;
using System.Text.RegularExpressions;

namespace P3.Driver.Camera.Driver
{
    /// <summary>
    /// String helpers for camera URLs. The URLs are not round-tripped through <see cref="Uri"/>,
    /// it re-escapes paths and queries that some cameras need verbatim.
    /// </summary>
    internal static class StreamUrl
    {
        private static readonly Regex UserInfo = new(@"^(?<scheme>[a-zA-Z][a-zA-Z0-9+.\-]*://)(?<user>[^@/]*@)?(?<rest>.*)$", RegexOptions.Compiled);

        /// <summary>
        /// Puts the credentials into the URL unless it has some already (or there are none)
        /// </summary>
        public static string WithCredentials(string url, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            url = url.Trim();
            var match = UserInfo.Match(url);
            if (!match.Success || match.Groups["user"].Success || string.IsNullOrEmpty(username))
            {
                return url;
            }

            var credentials = Uri.EscapeDataString(username);
            if (!string.IsNullOrEmpty(password))
            {
                credentials += ":" + Uri.EscapeDataString(password);
            }

            return $"{match.Groups["scheme"].Value}{credentials}@{match.Groups["rest"].Value}";
        }

        /// <summary>
        /// The go2rtc source of an RTSP stream. Two-way audio is not used by the visu, and some cameras
        /// misbehave when go2rtc requests the backchannel, so it is turned off unless the URL has options already.
        /// </summary>
        public static string ToGo2RtcSource(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            url = url.Trim();
            var isRtsp = url.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase);
            return isRtsp && !url.Contains('#') ? url + "#backchannel=0" : url;
        }

        /// <summary>
        /// For log lines: the URL without its credentials
        /// </summary>
        public static string Redact(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            var match = UserInfo.Match(url);
            return match.Success && match.Groups["user"].Success
                ? $"{match.Groups["scheme"].Value}***@{match.Groups["rest"].Value}"
                : url;
        }

        /// <summary>
        /// Host and port to probe for the online state: the RTSP port of the stream, or the given fallback
        /// </summary>
        public static bool TryGetEndpoint(string url, int defaultPort, out string host, out int port)
        {
            host = null;
            port = defaultPort;
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var match = UserInfo.Match(url.Trim());
            if (!match.Success)
            {
                return false;
            }

            var rest = match.Groups["rest"].Value;
            var end = rest.IndexOfAny(new[] { '/', '?', '#' });
            var authority = end >= 0 ? rest[..end] : rest;
            if (authority.Length == 0)
            {
                return false;
            }

            var scheme = match.Groups["scheme"].Value.ToLowerInvariant();
            port = scheme switch
            {
                "rtsp://" => 554,
                "rtsps://" => 322,
                "https://" => 443,
                "http://" => 80,
                _ => defaultPort
            };

            if (authority.StartsWith('['))
            {
                var close = authority.IndexOf(']');
                if (close < 0)
                {
                    return false;
                }
                host = authority[1..close];
                if (close + 2 < authority.Length && authority[close + 1] == ':' && int.TryParse(authority[(close + 2)..], out var v6Port))
                {
                    port = v6Port;
                }
                return true;
            }

            var colon = authority.LastIndexOf(':');
            if (colon > 0 && int.TryParse(authority[(colon + 1)..], out var p))
            {
                host = authority[..colon];
                port = p;
            }
            else
            {
                host = authority;
            }
            return host.Length > 0;
        }
    }
}
