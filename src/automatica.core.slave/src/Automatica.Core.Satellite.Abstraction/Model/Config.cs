namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class Config
    {
        public string ServerUrl { get; set; }
        public string ClientId { get; set; }

        /// <summary>
        /// Write-only: never returned by GET /webapi/config, an empty value on POST keeps the current key
        /// </summary>
        public string ClientKey { get; set; }

        /// <summary>
        /// GET only: whether a key is configured
        /// </summary>
        public bool HasClientKey { get; set; }

        public int Port { get; set; } = 5010;

        public string DockerTag { get; set; }

        /// <summary>
        /// Connect to the broker's TLS listener; null = automatic (switch to TLS once the core offers it)
        /// </summary>
        public bool? UseTls { get; set; }

        public int TlsPort { get; set; } = 8883;

        /// <summary>
        /// SHA-256 fingerprint of the core's broker certificate (file mqtt-broker-certificate.sha256 on the core)
        /// </summary>
        public string TlsFingerprint { get; set; }
    }
}
