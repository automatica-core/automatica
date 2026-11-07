using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace P3.Driver.Synology.Tests
{
    internal static class TestCertificates
    {
        // fixed validity, the tests never depend on the current date
        public static readonly DateTimeOffset NotBefore = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public static readonly DateTimeOffset NotAfter = new DateTimeOffset(2099, 12, 31, 0, 0, 0, TimeSpan.Zero);

        /// <summary>self-signed certificate (ephemeral key) for the validator unit tests</summary>
        public static X509Certificate2 CreateSelfSigned(string subject = "CN=diskstation")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(NotBefore, NotAfter);
        }

        /// <summary>
        /// self-signed server certificate for 127.0.0.1/localhost that SChannel can use (re-imported from PKCS#12, the key is removed on dispose)
        /// </summary>
        public static X509Certificate2 CreateServerCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(IPAddress.Loopback);
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

            using var ephemeral = request.CreateSelfSigned(NotBefore, NotAfter);
            return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
        }

        public static string WithColons(string hex)
        {
            var parts = new string[hex.Length / 2];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = hex.Substring(i * 2, 2);
            }
            return string.Join(":", parts);
        }
    }
}
