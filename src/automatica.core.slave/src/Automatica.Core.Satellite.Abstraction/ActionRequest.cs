using System;

namespace Automatica.Core.Satellite.Abstraction
{
    public enum SlaveAction
    {
        Start,
        Stop,
        Restart,
        Kill
    }

    public class ActionRequest
    {
        public SlaveAction Action { get; set; }
        public string ImageName { get; set; }
        public string Tag { get; set; }

        /// <summary>
        /// Not supported any more (a rootfs URL to import), requests with it are rejected; older cores always send
        /// the Docker Hub URL, which is ignored
        /// </summary>
        public string ImageSource { get; set; }

        public Guid Id { get; set; }

        /// <summary>
        /// Broker password of the plugin container (cores since the broker hardening)
        /// </summary>
        public string ClientKey { get; set; }

        /// <summary>
        /// Port of the core's TLS broker listener, 0 if none
        /// </summary>
        public int BrokerTlsPort { get; set; }

        /// <summary>
        /// SHA-256 fingerprint of the core's broker certificate
        /// </summary>
        public string BrokerFingerprint { get; set; }

        /// <summary>
        /// Digest (sha256:...) the image is pinned to, optional
        /// </summary>
        public string ImageDigest { get; set; }

        /// <summary>
        /// Host devices the plugin needs (checked against the satellite's allow-list)
        /// </summary>
        public string[] Devices { get; set; }

        /// <summary>
        /// Linux capabilities the plugin needs (checked against the satellite's allow-list)
        /// </summary>
        public string[] CapAdd { get; set; }
    }
}
