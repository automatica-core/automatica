using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Satellite.Abstraction.Model;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    public class UserAuthDataTests
    {
        private static string Pbkdf2Hash(string password, int iterations, byte[] salt = null)
        {
            salt ??= new byte[16];
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA512, 32);
            return $"{UserAuthData.HashPrefix}{iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static (string Hash, string Salt) LegacyHash(string password)
        {
            var salt = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
            var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, 10000, 32);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        [Fact]
        public void HashPassword_UsesPbkdf2Sha512With210kIterationsAndTheSaltInTheValue()
        {
            var hash = UserAuthData.HashPassword("a-long-password");
            var parts = hash.Split('$');

            Assert.Equal(4, parts.Length);
            Assert.Equal("pbkdf2-sha512", parts[0]);
            Assert.Equal("210000", parts[1]);
            Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
            Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_UsesDifferentSalts()
        {
            Assert.NotEqual(UserAuthData.HashPassword("same-password"), UserAuthData.HashPassword("same-password"));
        }

        [Fact]
        public void HashPassword_DoesNotContainThePassword()
        {
            Assert.DoesNotContain("plain-secret-pw", UserAuthData.HashPassword("plain-secret-pw"));
        }

        [Fact]
        public void VerifyPassword_UnicodePassword_RoundTrips()
        {
            var hash = UserAuthData.HashPassword("pässwört-ß-密码");

            Assert.True(UserAuthData.VerifyPassword("pässwört-ß-密码", hash, null, out _));
            Assert.False(UserAuthData.VerifyPassword("passwort-ss-密码", hash, null, out _));
        }

        [Fact]
        public void VerifyPassword_FewerIterationsThanCurrent_AsksForRehash()
        {
            var hash = Pbkdf2Hash("old-iterations-pw", 1000);

            Assert.True(UserAuthData.VerifyPassword("old-iterations-pw", hash, null, out var rehash));
            Assert.True(rehash);
        }

        [Fact]
        public void VerifyPassword_WrongPasswordOnOldHash_DoesNotAskForRehash()
        {
            var hash = Pbkdf2Hash("old-iterations-pw", 1000);

            Assert.False(UserAuthData.VerifyPassword("other", hash, null, out var rehash));
            Assert.False(rehash);
        }

        [Theory]
        [InlineData("pbkdf2-sha512$")]
        [InlineData("pbkdf2-sha512$210000$AAAA")]
        [InlineData("pbkdf2-sha512$abc$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
        [InlineData("pbkdf2-sha512$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
        [InlineData("pbkdf2-sha512$-5$AAAAAAAAAAAAAAAAAAAAAA==$AAAA")]
        [InlineData("pbkdf2-sha512$1000$not base64!$AAAA")]
        [InlineData("pbkdf2-sha512$1000$AAAAAAAAAAAAAAAAAAAAAA==$not base64!")]
        [InlineData("pbkdf2-sha512$1000$a$b$c")]
        public void VerifyPassword_MalformedHash_ReturnsFalseWithoutThrowing(string stored)
        {
            Assert.False(UserAuthData.VerifyPassword("anything", stored, null, out var rehash));
            Assert.False(rehash);
        }

        [Theory]
        [InlineData(null, "x")]
        [InlineData("", "x")]
        [InlineData("pw", null)]
        [InlineData("pw", "")]
        public void VerifyPassword_EmptyInputs_ReturnFalse(string password, string stored)
        {
            Assert.False(UserAuthData.VerifyPassword(password, stored, "c2FsdA==", out _));
        }

        [Fact]
        public void VerifyPassword_LegacyHashWithoutSalt_IsRejected()
        {
            var (hash, _) = LegacyHash("legacy-password");

            Assert.False(UserAuthData.VerifyPassword("legacy-password", hash, null, out _));
            Assert.False(UserAuthData.VerifyPassword("legacy-password", hash, "", out _));
        }

        [Fact]
        public void VerifyPassword_LegacyHash_IsAcceptedAndAsksForRehash()
        {
            var (hash, salt) = LegacyHash("legacy-password");

            Assert.True(UserAuthData.VerifyPassword("legacy-password", hash, salt, out var rehash));
            Assert.True(rehash);
        }

        [Fact]
        public void VerifyPassword_LegacyHashWithWrongSalt_IsRejected()
        {
            var (hash, _) = LegacyHash("legacy-password");

            Assert.False(UserAuthData.VerifyPassword("legacy-password", hash, Convert.ToBase64String(new byte[16]), out _));
        }

        [Fact]
        public void VerifyPassword_LegacyGarbage_ReturnsFalseWithoutThrowing()
        {
            Assert.False(UserAuthData.VerifyPassword("pw", "not base64!", "c2FsdA==", out _));
            Assert.False(UserAuthData.VerifyPassword("pw", "AAAA", "not base64!", out _));
        }

        [Fact]
        public void VerifyPassword_PlainTextPasswordInConfig_IsNotAccepted()
        {
            // a password typed into appsettings.json as plain text must not work
            Assert.False(UserAuthData.VerifyPassword("plain-text-pw", "plain-text-pw", null, out _));
            Assert.False(UserAuthData.VerifyPassword("plain-text-pw", "plain-text-pw", "c2FsdA==", out _));
        }

        [Theory]
        [InlineData("abc", "abc", true)]
        [InlineData("abc", "abd", false)]
        [InlineData("abc", "abcd", false)]
        [InlineData("", "", true)]
        [InlineData(null, "abc", false)]
        [InlineData("abc", null, false)]
        [InlineData(null, null, false)]
        public void FixedTimeEquals_ComparesValues(string a, string b, bool expected)
        {
            Assert.Equal(expected, UserAuthData.FixedTimeEquals(a, b));
        }

        [Fact]
        public void GenerateToken_IsUrlSafeAndRandom()
        {
            var tokens = Enumerable.Range(0, 50).Select(_ => UserAuthData.GenerateToken()).ToList();

            Assert.Equal(50, tokens.Distinct().Count());
            Assert.All(tokens, t =>
            {
                Assert.Equal(43, t.Length); // 32 bytes base64 without padding
                Assert.DoesNotContain('+', t);
                Assert.DoesNotContain('/', t);
                Assert.DoesNotContain('=', t);
            });
        }

        [Fact]
        public void GenerateToken_SetupTokenLength()
        {
            Assert.Equal(24, UserAuthData.GenerateToken(18).Length);
        }

        [Fact]
        public void Constants_MinimumPasswordLengthAndIterations()
        {
            Assert.Equal(10, UserAuthData.MinimumPasswordLength);
            Assert.True(UserAuthData.Iterations >= 210_000);
        }
    }
}
