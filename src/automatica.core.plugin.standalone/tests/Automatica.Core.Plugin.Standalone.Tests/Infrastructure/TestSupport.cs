using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Automatica.Core.Plugin.Standalone.Tests.Infrastructure
{
    /// <summary>
    /// Tests that change process-wide state: the TLS environment variables MqttConnection reads, and
    /// MqttMessageHandlers.ExitProcess
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class ProcessStateCollection
    {
        public const string Name = "plugin process state";
    }

    internal static class TestPorts
    {
        public static int Free()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    internal static class Eventually
    {
        public static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return true;
                }
                await Task.Delay(10);
            }
            return condition();
        }

        public static async Task Assert(Func<bool> condition, string message, TimeSpan? timeout = null)
        {
            Xunit.Assert.True(await WaitUntil(condition, timeout), message);
        }
    }

    internal sealed class ListLogger : ILogger
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new ConcurrentQueue<(LogLevel, string)>();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Enqueue((logLevel, formatter(state, exception)));
        }

        public bool Contains(LogLevel level, string fragment) => Entries.Any(e => e.Level == level && e.Message.Contains(fragment));
    }

    internal static class TestCertificates
    {
        private static readonly Lazy<byte[]> SharedPfx = new Lazy<byte[]>(() =>
        {
            using var cert = CreateServerCertificate();
            return cert.Export(X509ContentType.Pfx);
        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The same broker certificate for the whole run (one RSA key generation instead of one per test), as a
        /// fresh instance each call: the MQTTnet server disposes the certificate it was given when it stops
        /// </summary>
        public static X509Certificate2 SharedServerCertificate() => X509CertificateLoader.LoadPkcs12(SharedPfx.Value, null, X509KeyStorageFlags.Exportable);

        public static X509Certificate2 CreateServerCertificate(string cn = "automatica-test-core")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(IPAddress.Loopback);
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            // fixed validity window, valid for any test date between 2000 and 2099
            using var cert = request.CreateSelfSigned(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
            return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.Exportable);
        }

        public static string Fingerprint(X509Certificate2 cert) => Convert.ToHexString(SHA256.HashData(cert.RawData)).ToLowerInvariant();
    }

    /// <summary>
    /// Sets the environment the satellite passes to a plugin container and restores the previous values
    /// </summary>
    internal sealed class PluginEnvironment : IDisposable
    {
        private static readonly string[] Names = { "AUTOMATICA_SLAVE_TLS_PORT", "AUTOMATICA_SLAVE_TLS_FINGERPRINT" };
        private readonly string[] _previous;

        public PluginEnvironment(int tlsPort, string fingerprint)
        {
            _previous = Names.Select(Environment.GetEnvironmentVariable).ToArray();
            Environment.SetEnvironmentVariable(Names[0], tlsPort.ToString());
            Environment.SetEnvironmentVariable(Names[1], fingerprint);
        }

        public void Dispose()
        {
            for (var i = 0; i < Names.Length; i++)
            {
                Environment.SetEnvironmentVariable(Names[i], _previous[i]);
            }
        }
    }

    internal static class TestKeys
    {
        // made-up credentials, only valid inside these tests
        public const string SatelliteUser = "sat-test-1";
        public const string PluginPassword = "test-plugin-key-0000000001";
    }
}
