using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Automatica.Core.Satellite
{
    /// <summary>
    /// Per-install secrets of the satellite web interface, stored in the config directory (a volume in docker):
    /// the JWT signing key and the self-signed https certificate
    /// </summary>
    internal static class SatelliteSecrets
    {
        public const string JwtKeyFileName = "jwt-signing.key";
        public const string CertificateFileName = "satellite-certificate.pfx";

        /// <summary>
        /// For tests: a temp directory instead of the directory of the entry assembly
        /// </summary>
        internal static string ConfigDirectoryOverride { get; set; }

        public static string ConfigDirectory
        {
            get
            {
                if (ConfigDirectoryOverride != null)
                {
                    return ConfigDirectoryOverride;
                }
                var configDir = new FileInfo(Assembly.GetEntryAssembly()!.Location).DirectoryName!;
                var sub = Path.Combine(configDir, "config");
                return Directory.Exists(sub) ? sub : configDir;
            }
        }

        public static byte[] GetJwtKey()
        {
            var path = Path.Combine(ConfigDirectory, JwtKeyFileName);
            try
            {
                if (File.Exists(path))
                {
                    var existing = File.ReadAllBytes(path);
                    if (existing.Length >= 32)
                    {
                        return existing;
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not read {path}: {e.Message}");
            }

            var key = RandomNumberGenerator.GetBytes(32);
            TryWritePrivate(path, key);
            return key;
        }

        public static X509Certificate2 GetCertificate()
        {
            var path = Path.Combine(ConfigDirectory, CertificateFileName);
            if (File.Exists(path))
            {
                try
                {
                    return X509CertificateLoader.LoadPkcs12FromFile(path, null);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Could not load {path}, creating a new certificate: {e.Message}");
                }
            }

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=automatica-satellite", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            try
            {
                san.AddDnsName(Dns.GetHostName());
            }
            catch (Exception)
            {
                // localhost is enough
            }
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

            var now = DateTimeOffset.UtcNow;
            using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));
            var pfx = certificate.Export(X509ContentType.Pkcs12);
            TryWritePrivate(path, pfx);
            return X509CertificateLoader.LoadPkcs12(pfx, null);
        }

        private static void TryWritePrivate(string path, byte[] data)
        {
            try
            {
                var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }
                using var stream = new FileStream(path, options);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not persist {path}: {e.Message}");
            }
        }
    }
}
