using System;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Loxone.Miniserver.Driver;
using Xunit;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    public class FormatValueTests
    {
        private static string FormatUnder(string culture, object value)
        {
            var current = CultureInfo.CurrentCulture;
            var currentUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                return LoxoneMiniserverConnection.FormatValue(value);
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
                CultureInfo.CurrentUICulture = currentUi;
            }
        }

        public static TheoryData<object, string> NumberValues => new TheoryData<object, string>
        {
            { 1.5d, "1.5" },
            { -0.25d, "-0.25" },
            { 0.1f, "0.1" },
            { 2.75m, "2.75" },
            { 1234567.5d, "1234567.5" },
            { 1e21d, "1E+21" },
            { 42, "42" },
            { -7L, "-7" },
            { (byte)255, "255" },
            { 0d, "0" },
        };

        [Theory]
        [MemberData(nameof(NumberValues))]
        public void FormatValue_GermanCulture_UsesInvariantDecimalSeparator(object value, string expected)
        {
            Assert.Equal(expected, FormatUnder("de-DE", value));
        }

        [Theory]
        [MemberData(nameof(NumberValues))]
        public void FormatValue_FrenchCulture_HasNoGroupSeparatorOrComma(object value, string expected)
        {
            Assert.Equal(expected, FormatUnder("fr-FR", value));
        }

        [Theory]
        [InlineData(true, "on")]
        [InlineData(false, "off")]
        public void FormatValue_Bool_IsOnOff(bool value, string expected)
        {
            Assert.Equal(expected, FormatUnder("de-DE", value));
        }

        [Fact]
        public void FormatValue_String_IsPassedThrough()
        {
            Assert.Equal("pulse", LoxoneMiniserverConnection.FormatValue("pulse"));
        }

        [Fact]
        public void FormatValue_Null_IsEmpty()
        {
            Assert.Equal(string.Empty, LoxoneMiniserverConnection.FormatValue(null));
        }

        [Theory]
        [InlineData("10.0.0.5", 80, false, "http://10.0.0.5:80", "ws://10.0.0.5:80/ws/rfc6455")]
        [InlineData("miniserver.local", 443, true, "https://miniserver.local:443", "wss://miniserver.local:443/ws/rfc6455")]
        [InlineData("504f94a0b1c2.dyndns.loxonecloud.com", 8443, true, "https://504f94a0b1c2.dyndns.loxonecloud.com:8443", "wss://504f94a0b1c2.dyndns.loxonecloud.com:8443/ws/rfc6455")]
        public void TransportAddresses_UseSchemeMatchingTls(string address, int port, bool tls, string http, string ws)
        {
            Assert.Equal(http, LoxoneMiniserverConnection.GetHttpBaseAddress(address, port, tls));
            Assert.Equal(ws, LoxoneMiniserverConnection.GetWebSocketAddress(address, port, tls));
        }

        [Fact]
        public void Constructor_Legacy_UsesPlainTransportWithFallback()
        {
            var connection = new LoxoneMiniserverConnection("127.0.0.1", 80, "user", "password", NullLogger.Instance);

            Assert.False(connection.UseTls);
            Assert.False(connection.IsTlsActive);
            Assert.True(connection.AllowPlainFallback);
            Assert.Equal(LoxoneMiniserverConnection.DefaultTlsPort, connection.TlsPort);
            Assert.False(connection.IsAuthenticated);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_InvalidTlsPort_FallsBackToDefaultTlsPort(int tlsPort)
        {
            var connection = new LoxoneMiniserverConnection("127.0.0.1", 80, "user", "password", NullLogger.Instance, true, tlsPort, false);

            Assert.Equal(LoxoneMiniserverConnection.DefaultTlsPort, connection.TlsPort);
            Assert.True(connection.UseTls);
            Assert.True(connection.IsTlsActive);
            Assert.False(connection.AllowPlainFallback);
        }

        [Fact]
        public void Constructor_Tls_StartsOnTlsTransport()
        {
            var connection = new LoxoneMiniserverConnection("127.0.0.1", 80, "user", "password", NullLogger.Instance, true, 8443, true);

            Assert.True(connection.IsTlsActive);
            Assert.Equal(8443, connection.TlsPort);
            Assert.Equal(80, connection.Port);
        }
    }
}
