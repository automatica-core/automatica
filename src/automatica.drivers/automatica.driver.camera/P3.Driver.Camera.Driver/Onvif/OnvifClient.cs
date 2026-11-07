using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace P3.Driver.Camera.Driver.Onvif
{
    internal sealed record OnvifProfile(string Token, string Name, int Width, int Height, string Encoding)
    {
        public int Pixels => Width * Height;
    }

    internal sealed record OnvifStreams(string MainUri, string SubUri, string SnapshotUri);

    internal sealed record OnvifEvent(string Topic, string Name, bool Value);

    /// <summary>
    /// The few ONVIF calls the driver needs: stream and snapshot URIs of the profiles and the motion events (pull point).
    /// SOAP 1.2 with a WS-Security username token (password digest), the camera clock offset is taken into account,
    /// since cameras reject tokens that are off by more than a few seconds.
    /// </summary>
    internal sealed class OnvifClient
    {
        internal static readonly XNamespace Soap = "http://www.w3.org/2003/05/soap-envelope";
        internal static readonly XNamespace Device = "http://www.onvif.org/ver10/device/wsdl";
        internal static readonly XNamespace Media = "http://www.onvif.org/ver10/media/wsdl";
        internal static readonly XNamespace Schema = "http://www.onvif.org/ver10/schema";
        internal static readonly XNamespace Events = "http://www.onvif.org/ver10/events/wsdl";
        internal static readonly XNamespace Wsnt = "http://docs.oasis-open.org/wsn/b-2";
        internal static readonly XNamespace Wsa = "http://www.w3.org/2005/08/addressing";

        private const string Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private const string Wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const string DigestType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";
        private const string Base64Type = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-soap-message-security-1.0#Base64Binary";

        private readonly HttpClient _http;
        private readonly string _username;
        private readonly string _password;
        private readonly Func<DateTimeOffset> _now;

        private TimeSpan _clockOffset = TimeSpan.Zero;

        public OnvifClient(HttpClient http, string host, int port, string username, string password, Func<DateTimeOffset> now = null)
        {
            _http = http;
            _username = username;
            _password = password ?? "";
            _now = now ?? (() => DateTimeOffset.UtcNow);
            var authority = host.Contains(':') && !host.StartsWith('[') ? $"[{host}]" : host;
            DeviceServiceUri = new Uri($"http://{authority}:{(port <= 0 ? 80 : port)}/onvif/device_service");
        }

        public Uri DeviceServiceUri { get; }

        public Uri MediaServiceUri { get; private set; }

        public Uri EventServiceUri { get; private set; }

        /// <summary>
        /// Reads the camera clock (no authentication needed) and the service addresses
        /// </summary>
        public async Task ConnectAsync(CancellationToken token = default)
        {
            try
            {
                var time = await CallAsync(DeviceServiceUri, Device + "GetSystemDateAndTime", new XElement(Device + "GetSystemDateAndTime"), false, null, token);
                var cameraTime = ParseSystemTime(time);
                if (cameraTime.HasValue)
                {
                    _clockOffset = cameraTime.Value - _now();
                }
            }
            catch (OnvifException)
            {
                // some cameras want authentication even here, go on with the local clock
            }

            var capabilities = await CallAsync(DeviceServiceUri, Device + "GetCapabilities",
                new XElement(Device + "GetCapabilities", new XElement(Device + "Category", "All")), true, null, token);

            MediaServiceUri = ServiceAddress(capabilities, "Media") ?? DeviceServiceUri;
            EventServiceUri = ServiceAddress(capabilities, "Events");
        }

        internal TimeSpan ClockOffset => _clockOffset;

        public async Task<IReadOnlyList<OnvifProfile>> GetProfilesAsync(CancellationToken token = default)
        {
            var body = await CallAsync(MediaServiceUri ?? DeviceServiceUri, Media + "GetProfiles", new XElement(Media + "GetProfiles"), true, null, token);
            return ParseProfiles(body);
        }

        public async Task<string> GetStreamUriAsync(string profileToken, CancellationToken token = default)
        {
            var request = new XElement(Media + "GetStreamUri",
                new XElement(Media + "StreamSetup",
                    new XElement(Schema + "Stream", "RTP-Unicast"),
                    new XElement(Schema + "Transport", new XElement(Schema + "Protocol", "RTSP"))),
                new XElement(Media + "ProfileToken", profileToken));
            var body = await CallAsync(MediaServiceUri ?? DeviceServiceUri, Media + "GetStreamUri", request, true, null, token);
            return FirstLocal(body, "Uri")?.Value.Trim();
        }

        public async Task<string> GetSnapshotUriAsync(string profileToken, CancellationToken token = default)
        {
            var request = new XElement(Media + "GetSnapshotUri", new XElement(Media + "ProfileToken", profileToken));
            var body = await CallAsync(MediaServiceUri ?? DeviceServiceUri, Media + "GetSnapshotUri", request, true, null, token);
            return FirstLocal(body, "Uri")?.Value.Trim();
        }

        /// <summary>
        /// Main stream = the profile with the most pixels, sub stream = the one with the fewest (if there are two or more)
        /// </summary>
        public async Task<OnvifStreams> GetStreamsAsync(CancellationToken token = default)
        {
            var profiles = (await GetProfilesAsync(token)).Where(p => !string.IsNullOrEmpty(p.Token)).ToList();
            if (profiles.Count == 0)
            {
                return new OnvifStreams(null, null, null);
            }

            var main = profiles.OrderByDescending(p => p.Pixels).First();
            var sub = profiles.Count > 1 ? profiles.OrderBy(p => p.Pixels).First() : null;
            if (sub != null && sub.Token == main.Token)
            {
                sub = null;
            }

            var mainUri = await GetStreamUriAsync(main.Token, token);
            var subUri = sub != null ? await TryAsync(() => GetStreamUriAsync(sub.Token, token)) : null;
            var snapshot = await TryAsync(() => GetSnapshotUriAsync(main.Token, token));
            return new OnvifStreams(mainUri, subUri, snapshot);
        }

        #region events

        /// <returns>the address of the subscription</returns>
        public async Task<Uri> CreatePullPointSubscriptionAsync(TimeSpan termination, CancellationToken token = default)
        {
            if (EventServiceUri == null)
            {
                throw new OnvifException("The camera has no event service");
            }

            var request = new XElement(Events + "CreatePullPointSubscription",
                new XElement(Events + "InitialTerminationTime", ToDuration(termination)));
            var body = await CallAsync(EventServiceUri, Events + "CreatePullPointSubscription", request, true,
                "http://www.onvif.org/ver10/events/wsdl/EventPortType/CreatePullPointSubscriptionRequest", token);

            var address = body.Descendants().FirstOrDefault(e => e.Name.LocalName == "SubscriptionReference")?
                .Descendants().FirstOrDefault(e => e.Name.LocalName == "Address")?.Value.Trim();
            if (string.IsNullOrEmpty(address) || !Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                throw new OnvifException("The pull point subscription has no address");
            }
            return uri;
        }

        public async Task<IReadOnlyList<OnvifEvent>> PullMessagesAsync(Uri subscription, TimeSpan timeout, int limit, CancellationToken token = default)
        {
            var request = new XElement(Events + "PullMessages",
                new XElement(Events + "Timeout", ToDuration(timeout)),
                new XElement(Events + "MessageLimit", limit));
            var body = await CallAsync(subscription, Events + "PullMessages", request, true,
                "http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest", token, subscription);
            return ParseNotifications(body);
        }

        public Task RenewAsync(Uri subscription, TimeSpan termination, CancellationToken token = default)
        {
            var request = new XElement(Wsnt + "Renew", new XElement(Wsnt + "TerminationTime", ToDuration(termination)));
            return CallAsync(subscription, Wsnt + "Renew", request, true, "http://docs.oasis-open.org/wsn/bw-2/SubscriptionManager/RenewRequest", token, subscription);
        }

        public Task UnsubscribeAsync(Uri subscription, CancellationToken token = default)
        {
            return CallAsync(subscription, Wsnt + "Unsubscribe", new XElement(Wsnt + "Unsubscribe"), true,
                "http://docs.oasis-open.org/wsn/bw-2/SubscriptionManager/UnsubscribeRequest", token, subscription);
        }

        #endregion

        #region soap

        private async Task<XElement> CallAsync(Uri uri, XName operation, XElement request, bool authenticate, string action, CancellationToken token, Uri to = null)
        {
            var envelope = BuildEnvelope(request, authenticate ? _username : null, _password, _now() + _clockOffset, RandomNumberGenerator.GetBytes(16), action, to);

            using var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8);
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml") { CharSet = "utf-8" };
            if (action != null)
            {
                message.Content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", $"\"{action}\""));
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(message, token);
            }
            catch (HttpRequestException ex)
            {
                throw new OnvifException($"{operation.LocalName}: {ex.Message}", ex);
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync(token);
                XDocument doc;
                try
                {
                    doc = XDocument.Parse(text);
                }
                catch (Exception ex) when (ex is System.Xml.XmlException)
                {
                    throw new OnvifException($"{operation.LocalName}: HTTP {(int)response.StatusCode}, no SOAP response", ex);
                }

                var body = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
                var fault = body?.Elements().FirstOrDefault(e => e.Name.LocalName == "Fault");
                if (fault != null || !response.IsSuccessStatusCode)
                {
                    var reason = fault?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Text")?.Value
                                 ?? fault?.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value
                                 ?? response.ReasonPhrase;
                    var subcode = fault?.Descendants().Where(e => e.Name.LocalName == "Value").Select(e => e.Value).LastOrDefault();
                    throw new OnvifException($"{operation.LocalName}: {reason}{(subcode != null ? $" ({subcode})" : "")}")
                    {
                        IsNotAuthorized = subcode != null && subcode.Contains("NotAuthorized", StringComparison.OrdinalIgnoreCase)
                                          || response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    };
                }
                return body ?? throw new OnvifException($"{operation.LocalName}: empty SOAP response");
            }
        }

        internal static XDocument BuildEnvelope(XElement request, string username, string password, DateTimeOffset created, byte[] nonce, string action, Uri to)
        {
            var header = new XElement(Soap + "Header");
            if (action != null)
            {
                header.Add(new XElement(Wsa + "Action", action));
            }
            if (to != null)
            {
                header.Add(new XElement(Wsa + "To", to.ToString()));
            }
            if (!string.IsNullOrEmpty(username))
            {
                var createdText = created.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
                XNamespace wsse = Wsse;
                XNamespace wsu = Wsu;
                header.Add(new XElement(wsse + "Security", new XAttribute(Soap + "mustUnderstand", "1"),
                    new XElement(wsse + "UsernameToken",
                        new XElement(wsse + "Username", username),
                        new XElement(wsse + "Password", new XAttribute("Type", DigestType), PasswordDigest(nonce, createdText, password)),
                        new XElement(wsse + "Nonce", new XAttribute("EncodingType", Base64Type), Convert.ToBase64String(nonce)),
                        new XElement(wsu + "Created", createdText))));
            }

            return new XDocument(new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "s", Soap.NamespaceName),
                header,
                new XElement(Soap + "Body", request)));
        }

        /// <summary>
        /// Base64(SHA1(nonce + created + password)), WS-Security username token profile 1.0
        /// </summary>
        internal static string PasswordDigest(byte[] nonce, string created, string password)
        {
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? "");
            var buffer = new byte[nonce.Length + createdBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(nonce, 0, buffer, 0, nonce.Length);
            Buffer.BlockCopy(createdBytes, 0, buffer, nonce.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, buffer, nonce.Length + createdBytes.Length, passwordBytes.Length);
            return Convert.ToBase64String(SHA1.HashData(buffer));
        }

        #endregion

        #region parsing

        internal static DateTimeOffset? ParseSystemTime(XElement body)
        {
            var utc = body.Descendants().FirstOrDefault(e => e.Name.LocalName == "UTCDateTime");
            if (utc == null)
            {
                return null;
            }

            int Part(string parent, string name) =>
                int.TryParse(utc.Descendants().FirstOrDefault(e => e.Name.LocalName == parent)?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value,
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : -1;

            var year = Part("Date", "Year");
            var month = Part("Date", "Month");
            var day = Part("Date", "Day");
            var hour = Part("Time", "Hour");
            var minute = Part("Time", "Minute");
            var second = Part("Time", "Second");
            if (year < 1970 || month < 1 || day < 1 || hour < 0 || minute < 0 || second < 0)
            {
                return null;
            }

            try
            {
                return new DateTimeOffset(year, month, day, hour, minute, Math.Min(second, 59), TimeSpan.Zero);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        internal static Uri ServiceAddress(XElement capabilities, string service)
        {
            var address = capabilities.Descendants().FirstOrDefault(e => e.Name.LocalName == service)?
                .Elements().FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value.Trim();
            return !string.IsNullOrEmpty(address) && Uri.TryCreate(address, UriKind.Absolute, out var uri) ? uri : null;
        }

        internal static IReadOnlyList<OnvifProfile> ParseProfiles(XElement body)
        {
            var result = new List<OnvifProfile>();
            foreach (var profile in body.Descendants().Where(e => e.Name.LocalName == "Profiles"))
            {
                var token = profile.Attribute("token")?.Value;
                var name = profile.Elements().FirstOrDefault(e => e.Name.LocalName == "Name")?.Value;
                var encoder = profile.Elements().FirstOrDefault(e => e.Name.LocalName == "VideoEncoderConfiguration");
                var resolution = encoder?.Elements().FirstOrDefault(e => e.Name.LocalName == "Resolution");
                int.TryParse(resolution?.Elements().FirstOrDefault(e => e.Name.LocalName == "Width")?.Value, out var width);
                int.TryParse(resolution?.Elements().FirstOrDefault(e => e.Name.LocalName == "Height")?.Value, out var height);
                var encoding = encoder?.Elements().FirstOrDefault(e => e.Name.LocalName == "Encoding")?.Value;
                result.Add(new OnvifProfile(token, name, width, height, encoding));
            }
            return result;
        }

        /// <summary>
        /// Flattens the notifications into (topic, item name, value) for every boolean simple item of the data
        /// </summary>
        internal static IReadOnlyList<OnvifEvent> ParseNotifications(XElement body)
        {
            var result = new List<OnvifEvent>();
            foreach (var notification in body.Descendants().Where(e => e.Name.LocalName == "NotificationMessage"))
            {
                var topic = notification.Elements().FirstOrDefault(e => e.Name.LocalName == "Topic")?.Value.Trim() ?? "";
                var message = notification.Elements().FirstOrDefault(e => e.Name.LocalName == "Message");
                var data = message?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Data");
                if (data == null)
                {
                    continue;
                }

                foreach (var item in data.Elements().Where(e => e.Name.LocalName == "SimpleItem"))
                {
                    var name = item.Attribute("Name")?.Value;
                    var value = item.Attribute("Value")?.Value?.Trim();
                    if (name == null || value == null)
                    {
                        continue;
                    }

                    if (bool.TryParse(value, out var flag))
                    {
                        result.Add(new OnvifEvent(topic, name, flag));
                    }
                    else if (value == "1" || value == "0")
                    {
                        result.Add(new OnvifEvent(topic, name, value == "1"));
                    }
                }
            }
            return result;
        }

        private static XElement FirstLocal(XElement body, string localName) => body.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

        internal static string ToDuration(TimeSpan span) => System.Xml.XmlConvert.ToString(span);

        private static async Task<T> TryAsync<T>(Func<Task<T>> call) where T : class
        {
            try
            {
                return await call();
            }
            catch (OnvifException)
            {
                return null;
            }
        }

        #endregion
    }

    internal sealed class OnvifException : Exception
    {
        public OnvifException(string message, Exception inner = null) : base(message, inner)
        {
        }

        public bool IsNotAuthorized { get; init; }
    }
}
