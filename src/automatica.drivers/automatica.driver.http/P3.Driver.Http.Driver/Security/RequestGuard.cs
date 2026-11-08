using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Http.Driver.Security
{
    /// <summary>
    /// SSRF protection of the virtual HTTP inputs and outputs. The URLs are configured by administrators and point to
    /// devices in the local network, so private addresses are allowed. Blocked are:
    /// <list type="bullet">
    /// <item>other schemes than http and https, and credentials in the URL (they belong into the password property)</item>
    /// <item>loopback (the core itself, its database, local services) unless "allow local host" is on</item>
    /// <item>link-local (169.254.0.0/16 with the cloud metadata service, fe80::/10), unspecified, multicast and broadcast</item>
    /// </list>
    /// The address check runs on the resolved IP right before the connection is made (<see cref="CreateHandler"/>), so a
    /// DNS name that resolves to a blocked address (DNS rebinding) is caught as well. Redirects are not followed.
    /// </summary>
    internal static class RequestGuard
    {
        /// <summary>
        /// Largest response body that is read, bigger responses are cut off with an error
        /// </summary>
        public const int MaxResponseBytes = 1024 * 1024;

        /// <summary>
        /// Validates a configured URL, throws <see cref="RequestBlockedException"/> with a reason for the log
        /// </summary>
        public static Uri ValidateUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new RequestBlockedException("no URL configured");
            }
            if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
            {
                throw new RequestBlockedException("the URL is not valid");
            }
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new RequestBlockedException($"only http and https are allowed, not {uri.Scheme}");
            }
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new RequestBlockedException("the URL must not contain credentials, use the user and password properties");
            }
            if (string.IsNullOrEmpty(uri.Host))
            {
                throw new RequestBlockedException("the URL has no host");
            }
            return uri;
        }

        /// <summary>
        /// Combines the address of an output device with a command: a command with its own http(s) URL is used as is,
        /// otherwise it is appended to the address ("/relay/0?turn=on" or "relay/0?turn=on").
        /// </summary>
        public static Uri Combine(string address, string command)
        {
            command = command?.Trim() ?? "";
            if (command.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || command.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateUrl(command);
            }

            var baseText = (address ?? "").Trim();
            if (command.Length > 0)
            {
                baseText = baseText.TrimEnd('/') + "/" + command.TrimStart('/');
            }
            return ValidateUrl(baseText);
        }

        /// <summary>
        /// Whether a connection to <paramref name="address"/> is allowed
        /// </summary>
        public static bool IsAllowed(IPAddress address, bool allowLoopback)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address))
            {
                return allowLoopback;
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = address.GetAddressBytes();
                // 0.0.0.0/8 "this network", 169.254.0.0/16 link-local (metadata), 224.0.0.0/4 multicast, 240.0.0.0/4 reserved + broadcast
                if (b[0] == 0 || (b[0] == 169 && b[1] == 254) || b[0] >= 224)
                {
                    return false;
                }
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal ||
                    address.IsIPv6Multicast || address.IsIPv6SiteLocal)
                {
                    return false;
                }
                var b = address.GetAddressBytes();
                // IPv4-compatible / NAT64 well-known prefix with an embedded IPv4 address: check the IPv4 part
                if (b.Take(12).All(x => x == 0) || (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b))
                {
                    return IsAllowed(new IPAddress(b.Skip(12).ToArray()), allowLoopback);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves <paramref name="host"/> and returns the addresses that may be connected to.
        /// Throws <see cref="RequestBlockedException"/> when every address is blocked.
        /// </summary>
        public static async Task<IReadOnlyList<IPAddress>> ResolveAllowedAsync(string host, bool allowLoopback,
            Func<string, CancellationToken, Task<IPAddress[]>> resolve, CancellationToken token)
        {
            IPAddress[] addresses;
            if (IPAddress.TryParse(host.Trim('[', ']'), out var literal))
            {
                addresses = new[] { literal };
            }
            else
            {
                addresses = await resolve(host, token).ConfigureAwait(false);
            }

            var allowed = addresses.Where(a => IsAllowed(a, allowLoopback)).ToList();
            if (allowed.Count == 0)
            {
                throw new RequestBlockedException(addresses.Length == 0
                    ? $"{host} could not be resolved"
                    : $"{host} resolves to a blocked address ({string.Join(", ", addresses.Select(a => a.ToString()))})");
            }
            return allowed;
        }

        /// <summary>
        /// A handler that checks every connection with <see cref="IsAllowed"/>, follows no redirects and uses no proxy
        /// (a proxy would hide the target address from the check).
        /// </summary>
        public static SocketsHttpHandler CreateHandler(Func<bool> allowLoopback, Func<string, CancellationToken, Task<IPAddress[]>> resolve = null)
        {
            resolve ??= (host, token) => Dns.GetHostAddressesAsync(host, token);

            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(10),
                MaxResponseHeadersLength = 64,
                ConnectCallback = async (context, token) =>
                {
                    var addresses = await ResolveAllowedAsync(context.DnsEndPoint.Host, allowLoopback(), resolve, token).ConfigureAwait(false);

                    Exception last = null;
                    foreach (var address in addresses)
                    {
                        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                        try
                        {
                            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token).ConfigureAwait(false);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch (Exception ex) when (ex is SocketException or IOException)
                        {
                            socket.Dispose();
                            last = ex;
                        }
                    }
                    throw last ?? new RequestBlockedException("no address to connect to");
                }
            };
        }

        /// <summary>
        /// Reads at most <see cref="MaxResponseBytes"/> of the body as text (UTF-8 unless the response names a charset)
        /// </summary>
        public static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken token)
        {
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
            {
                throw new RequestBlockedException($"the response is larger than {MaxResponseBytes / 1024} KB");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(chunk, token).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes)
                {
                    throw new RequestBlockedException($"the response is larger than {MaxResponseBytes / 1024} KB");
                }
                buffer.Write(chunk, 0, read);
            }

            var charset = response.Content.Headers.ContentType?.CharSet;
            var encoding = System.Text.Encoding.UTF8;
            if (!string.IsNullOrWhiteSpace(charset))
            {
                try
                {
                    encoding = System.Text.Encoding.GetEncoding(charset.Trim('"'));
                }
                catch (ArgumentException)
                {
                    // unknown charset: UTF-8
                }
            }
            return encoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        }

        /// <summary>
        /// Scheme, host, port and path of a URL for the log: no query (it may carry tokens), no credentials
        /// </summary>
        public static string ForLog(Uri uri)
        {
            return uri == null ? "" : uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        }
    }

    internal sealed class RequestBlockedException : Exception
    {
        public RequestBlockedException(string message) : base(message)
        {
        }
    }
}
