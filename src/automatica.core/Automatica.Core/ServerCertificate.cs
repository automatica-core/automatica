using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;

namespace Automatica.Core
{
    /// <summary>
    /// The TLS certificate of the https port. Every installation gets its own self-signed certificate (created on the
    /// first start, stored in the persistent directory), the private key is never shipped with the software.
    /// The subject and issuer are "localhost": the app accepts self-signed certificates issued to/by localhost.
    /// To use an own certificate, replace the file (PKCS#12 without password) and restart.
    /// </summary>
    internal static class ServerCertificate
    {
        public const string FileName = "server-certificate.pfx";
        private const string SubjectName = "CN=localhost";

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

                var path = Path.Combine(ServerInfo.GetPersistentDirectory(), FileName);

                if (File.Exists(path))
                {
                    try
                    {
                        _certificate = X509CertificateLoader.LoadPkcs12FromFile(path, null);
                        return _certificate;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Could not load the server certificate {path}, create a new one");
                    }
                }

                var pfx = Create();
                try
                {
                    Write(path, pfx);
                    logger.LogInformation($"Created a new self-signed server certificate {path}");
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // not persisted: a new certificate on every start, still unique to this installation
                    logger.LogError(e, $"Could not persist the server certificate to {path}");
                }

                _certificate = X509CertificateLoader.LoadPkcs12(pfx, null);
                return _certificate;
            }
        }

        private static byte[] Create()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(SubjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            san.AddIpAddress(IPAddress.IPv6Loopback);
            try
            {
                san.AddDnsName(Dns.GetHostName());
            }
            catch (Exception)
            {
                // no host name, localhost is enough
            }
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            var now = DateTimeOffset.UtcNow;
            using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));

            // exported and loaded again: an ephemeral key does not work with SslStream on Windows
            return certificate.Export(X509ContentType.Pkcs12);
        }

        private static void Write(string path, byte[] pfx)
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

            using (var stream = new FileStream(path, options))
            {
                stream.Write(pfx, 0, pfx.Length);
            }
        }
    }
}
