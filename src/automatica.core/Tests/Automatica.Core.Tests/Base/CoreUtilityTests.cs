using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cryptography;
using Automatica.Core.Driver.Utility;
using Automatica.Core.Runtime.RemoteConnect.Frp;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Automatica.Core.Tests.Utilities
{
    public class DriverUtilsTests
    {
        [Theory]
        [InlineData(0x12, 0x34, 0x1234)]
        [InlineData(0x00, 0xFF, 0x00FF)]
        [InlineData(0xFF, 0x00, 0xFF00)]
        [InlineData(0x00, 0x00, 0x0000)]
        [InlineData(0x80, 0x01, 0x8001)]
        public void GetUShort_FirstByteIsTheHighByte(byte high, byte low, int expected)
        {
            Assert.Equal((ushort)expected, Utils.GetUShort(high, low));
        }

        [Fact]
        public void ByteArrayToString_SpaceSeparatedUpperHex()
        {
            Assert.Equal("00 0A FF", Utils.ByteArrayToString(new byte[] { 0x00, 0x0A, 0xFF }));
            Assert.Equal("01 02", Utils.ByteArrayToString(new ReadOnlySpan<byte>(new byte[] { 1, 2 })));
            Assert.Equal("01 02", Utils.ByteArrayToString(new ReadOnlyMemory<byte>(new byte[] { 1, 2 })));
            Assert.Equal(String.Empty, Utils.ByteArrayToString((byte[])null));
            Assert.Equal(String.Empty, Utils.ByteArrayToString(Array.Empty<byte>()));
        }

        [Fact]
        public void StringToByteArray_ParsesHexPairs_InBothCases()
        {
            Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, Utils.StringToByteArray("deadBEEF"));
            Assert.Empty(Utils.StringToByteArray(""));
        }

        [Fact]
        public void StringToByteArray_InvalidHex_Throws()
        {
            Assert.Throws<FormatException>(() => Utils.StringToByteArray("zz"));
        }

        [Fact]
        public void ReverseWithLookupTable_ReversesTheBitsOfEveryByte()
        {
            for (var i = 0; i < 256; i++)
            {
                var expected = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    if ((i & (1 << bit)) != 0)
                    {
                        expected |= 1 << (7 - bit);
                    }
                }
                Assert.Equal((byte)expected, Utils.ReverseWithLookupTable((byte)i));
            }
        }

        [Fact]
        public void Shifts_AreTruncatedToAByte()
        {
            Assert.Equal(0x12, Utils.ShiftRight((ushort)0x1234, 8));
            Assert.Equal(0x34, Utils.ShiftRight(0x1234, 0));
            Assert.Equal(0x40, Utils.ShiftLeft((ushort)0x0110, 2));
            Assert.Equal(0x00, Utils.ShiftLeft(0x80, 1));
        }

        [Fact]
        public void BitHelpers_SetAndClearBits()
        {
            Assert.Equal(0b1010_0000, Utils.SetBitsTo0(0b1111_0000, 0b0101_0000));
            Assert.Equal(0b0000_0101, Utils.SetBitsTo1(0b0000_0001, 2));
            Assert.Equal(0b0000_0001, Utils.SetBitIfTrue(0b0000_0001, 3, false));
            Assert.Equal(0b0000_1001, Utils.SetBitIfTrue(0b0000_0001, 3, true));
            for (byte bit = 0; bit < 8; bit++)
            {
                Assert.Equal(1, Utils.BitValue((byte)(1 << bit), bit));
                Assert.Equal(0, Utils.BitValue((byte)~(1 << bit), bit));
            }
            Assert.Throws<ArgumentException>(() => Utils.BitValue(0xFF, 8));
        }
    }

    public class HashTests
    {
        [Fact]
        public void CalculateSha1_MatchesTheKnownVector()
        {
            // FIPS 180 test vector
            Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", "abc".CalculateSha1());
            Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", "".CalculateSha1());
        }

        [Fact]
        public void CalculateHmacSha1_MatchesRfc2202()
        {
            Assert.Equal("effcdf6ae5eb2fa2d27416d5f184df9c259a7c79", "what do ya want for nothing?".CalculateHmacSha1(System.Text.Encoding.ASCII.GetBytes("Jefe")));
        }

        [Fact]
        public void ToHex_HonoursTheCase()
        {
            Assert.Equal("0aff", new byte[] { 0x0A, 0xFF }.ToHex(false));
            Assert.Equal("0AFF", new byte[] { 0x0A, 0xFF }.ToHex(true));
            Assert.Equal("", Array.Empty<byte>().ToHex(true));
        }
    }

    public class FrpcCredentialsTests
    {
        private static bool EnvironmentTokenIsSet => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(FrpcCredentials.FrpsTokenEnv));

        private static IConfiguration Config(string token) => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { FrpcCredentials.FrpsTokenConfigKey, token } }).Build();

        [Fact]
        public void AdminPassword_IsRandomPerInstance()
        {
            var a = new FrpcCredentials(Config(null));
            var b = new FrpcCredentials(Config(null));

            Assert.Equal("automatica", a.AdminUser);
            Assert.Equal(48, a.AdminPassword.Length);
            Assert.NotEqual(a.AdminPassword, b.AdminPassword);
            Assert.Equal(a.AdminPassword, a.AdminPassword);
        }

        [Fact]
        public void FrpsToken_FromTheConfiguration_WinsOverTheCloudToken()
        {
            if (EnvironmentTokenIsSet)
            {
                return;
            }
            var credentials = new FrpcCredentials(Config("configured-made-up-token"));

            credentials.SetCloudToken("cloud-made-up-token");

            Assert.Equal("configured-made-up-token", credentials.FrpsToken);
        }

        [Fact]
        public void FrpsToken_FallsBackToTheCloudToken_AndIgnoresEmptyUpdates()
        {
            if (EnvironmentTokenIsSet)
            {
                return;
            }
            var credentials = new FrpcCredentials(Config(null));
            Assert.Equal(String.Empty, credentials.FrpsToken);

            credentials.SetCloudToken("cloud-made-up-token");
            credentials.SetCloudToken(null);
            credentials.SetCloudToken("");

            Assert.Equal("cloud-made-up-token", credentials.FrpsToken);
        }

        [Fact]
        public void FrpsToken_WithoutConfiguration_IsEmpty()
        {
            if (EnvironmentTokenIsSet)
            {
                return;
            }

            Assert.Equal(String.Empty, new FrpcCredentials(null).FrpsToken);
        }
    }
}
