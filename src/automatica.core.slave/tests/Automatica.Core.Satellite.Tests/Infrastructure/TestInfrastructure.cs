using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Satellite.Tests.Infrastructure
{
    internal static class TestPorts
    {
        /// <summary>
        /// An ephemeral loopback port that is free right now
        /// </summary>
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
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        public static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
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

        public static async Task<bool> WaitUntil(Func<Task<bool>> condition, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            while (DateTime.UtcNow < deadline)
            {
                if (await condition())
                {
                    return true;
                }
                await Task.Delay(10);
            }
            return await condition();
        }

        public static async Task Assert(Func<bool> condition, string message, TimeSpan? timeout = null)
        {
            Xunit.Assert.True(await WaitUntil(condition, timeout), message);
        }

        public static async Task Assert(Func<Task<bool>> condition, string message, TimeSpan? timeout = null)
        {
            Xunit.Assert.True(await WaitUntil(condition, timeout), message);
        }
    }

    internal sealed class LogEntry
    {
        public LogLevel Level { get; init; }
        public string Category { get; init; }
        public string Message { get; init; }
        public override string ToString() => $"{Level} {Category}: {Message}";
    }

    /// <summary>
    /// Collects every log message, for assertions on what the satellite logs
    /// </summary>
    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

        public void Dispose()
        {
        }

        public ILogger<T> CreateLogger<T>() => new Logger<T>(new SingleProviderFactory(this));

        public bool Contains(LogLevel level, string fragment) =>
            Entries.Any(e => e.Level == level && e.Message.Contains(fragment, StringComparison.Ordinal));

        private sealed class SingleProviderFactory : ILoggerFactory
        {
            private readonly CapturingLoggerProvider _provider;
            public SingleProviderFactory(CapturingLoggerProvider provider) => _provider = provider;
            public ILogger CreateLogger(string categoryName) => _provider.CreateLogger(categoryName);
            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider _provider;
            private readonly string _category;

            public CapturingLogger(CapturingLoggerProvider provider, string category)
            {
                _provider = provider;
                _category = category;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _provider.Entries.Enqueue(new LogEntry { Level = logLevel, Category = _category, Message = formatter(state, exception) });
            }
        }
    }

    internal static class TestCertificates
    {
        private static readonly Lazy<byte[]> SharedPfx = new(() =>
        {
            using var cert = CreateServerCertificate();
            return cert.Export(X509ContentType.Pfx);
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The same broker certificate for the whole run (one RSA key generation instead of one per test), as a
        /// fresh instance each call so every owner can dispose its own
        /// </summary>
        public static X509Certificate2 SharedServerCertificate() => X509CertificateLoader.LoadPkcs12(SharedPfx.Value, null, X509KeyStorageFlags.Exportable);

        public static X509Certificate2 CreateServerCertificate(string cn = "automatica-test-broker")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(IPAddress.Loopback);
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            // fixed validity, independent of the test run's date as long as it is between 2000 and 2099
            using var cert = request.CreateSelfSigned(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
            return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.Exportable);
        }

        public static string Fingerprint(X509Certificate2 cert) => Convert.ToHexString(SHA256.HashData(cert.RawData)).ToLowerInvariant();
    }

    internal static class TestKeys
    {
        // made-up keys, only valid inside these tests
        public const string SatelliteKey = "test-satellite-key-0000000001";
        public const string PluginKey = "test-plugin-key-0000000001";
        public static readonly string Digest = "sha256:" + new string('b', 64);
    }

    internal static class CancellationTokens
    {
        public static CancellationToken Timeout(int seconds = 10) => new CancellationTokenSource(TimeSpan.FromSeconds(seconds)).Token;
    }
}
