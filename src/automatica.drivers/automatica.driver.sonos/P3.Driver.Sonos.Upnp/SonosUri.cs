using System;

namespace P3.Driver.Sonos.Upnp
{
    public class SonosUri
    {
        public static readonly int PortNumber = 1400;

        public string IpAddress { get; }
        public string Path { get; }

        public SonosUri(string ipAddress, string path)
        {
            if(string.IsNullOrEmpty(ipAddress))
                throw new ArgumentException("IP Address cannot be null or empty.", nameof(ipAddress));

            IpAddress = ipAddress;

            if (string.IsNullOrEmpty(path))
            {
                Path = string.Empty;
            }
            else
            {
                Path = path.StartsWith("/") ? path.Substring(1) : path;
            }
        }

        public Uri ToUri()
        {
            return new Uri(ToString());
        }

        public override string ToString()
        {
            return $"http://{IpAddress}:{PortNumber}/{Path}";
        }
    }
}