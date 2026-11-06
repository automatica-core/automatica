using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// The TLS certificate of the MQTT broker (port 8883). Self-signed and unique per installation, created on the first
    /// start in the persistent directory. Satellites and plugins don't validate the name, they pin the SHA-256
    /// fingerprint of this certificate (see <see cref="Fingerprint"/>), which the core sends them in the start action and
    /// writes next to the certificate (<see cref="FingerprintFileName"/>) so it can be entered on the satellite.
    /// To use an own certificate, replace the file (PKCS#12 without password) and restart.
    /// </summary>
    internal static class MqttBrokerCertificate
    {
        public const string FileName = "mqtt-broker-certificate.pfx";
        public const string FingerprintFileName = "mqtt-broker-certificate.sha256";
        private const string SubjectName = "CN=automatica-mqtt-broker";

        private static readonly object Lock = new object();
        private static X509Certificate2 _certificate;

        public static X509Certificate2 Get(ILogger logger)
        {
            lock (Lock)
            {
                if (_certificate != null)
                {
                    return _certificate;
                }

                var dir = ServerInfo.GetPersistentDirectory();
                var path = Path.Combine(dir, FileName);

                if (File.Exists(path))
                {
                    try
                    {
                        _certificate = X509CertificateLoader.LoadPkcs12FromFile(path, null);
                        WriteFingerprint(dir, logger);
                        return _certificate;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Could not load the mqtt broker certificate {path}, create a new one");
                    }
                }

                var pfx = Create();
                try
                {
                    WritePrivate(path, pfx);
                    logger.LogInformation($"Created a new self-signed mqtt broker certificate {path}");
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(e, $"Could not persist the mqtt broker certificate to {path}");
                }

                _certificate = X509CertificateLoader.LoadPkcs12(pfx, null);
                WriteFingerprint(dir, logger);
                return _certificate;
            }
        }

        /// <summary>
        /// SHA-256 over the DER encoded certificate, lowercase hex without separators
        /// </summary>
        public static string Fingerprint(X509Certificate2 certificate)
        {
            return Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();
        }

        private static void WriteFingerprint(string dir, ILogger logger)
        {
            var fingerprint = Fingerprint(_certificate);
            logger.LogInformation($"MQTT broker TLS certificate fingerprint (sha256): {fingerprint}");
            try
            {
                File.WriteAllText(Path.Combine(dir, FingerprintFileName), fingerprint + Environment.NewLine);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(e, "Could not write the mqtt broker certificate fingerprint");
            }
        }

        private static byte[] Create()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(SubjectName, ecdsa, HashAlgorithmName.SHA256);

            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            try
            {
                san.AddDnsName(Dns.GetHostName());
            }
            catch (Exception)
            {
                // the name is not validated by the clients (they pin the fingerprint)
            }
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            var now = DateTimeOffset.UtcNow;
            using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(20));

            // exported and loaded again: an ephemeral key does not work with SslStream on Windows
            return certificate.Export(X509ContentType.Pkcs12);
        }

        private static void WritePrivate(string path, byte[] pfx)
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None
            };

            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using var stream = new FileStream(path, options);
            stream.Write(pfx, 0, pfx.Length);
        }
    }
}
