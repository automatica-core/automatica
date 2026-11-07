using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Abstraction.Model;
using Automatica.Core.Satellite.Tests.Infrastructure;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    /// <summary>
    /// The satellite web api through the real Startup pipeline on a TestServer
    /// </summary>
    [Collection(SatelliteStaticsCollection.Name)]
    public class SatelliteWebApiTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private static Dictionary<string, string> SetUpUser(string password = SatelliteWebHost.AdminPassword) => new()
        {
            { "user", SatelliteWebHost.AdminUser },
            { "password", UserAuthData.HashPassword(password) },
            { "salt", "" }
        };

        private static string Token(byte[] key, string issuer = "automatica-satellite", string audience = "automatica-satellite",
            DateTime? notBefore = null, DateTime? expires = null, string algorithm = SecurityAlgorithms.HmacSha256Signature)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin"), new Claim(ClaimTypes.Name, "admin") }),
                Issuer = issuer,
                Audience = audience,
                NotBefore = notBefore,
                Expires = expires ?? DateTime.UtcNow.AddHours(1),
                IssuedAt = notBefore,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), algorithm)
            });
            return handler.WriteToken(token);
        }

        // ---- setup

        [Fact]
        public async Task Ready_BeforeSetup_IsFalse()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.Get("/webapi/ready");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ready\":false", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Start_WithoutUser_LogsASetupToken()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            Assert.False(String.IsNullOrEmpty(host.SetupToken));
            Assert.Equal(24, host.SetupToken.Length);
        }

        [Fact]
        public async Task Setup_WithoutToken_Returns401AndCreatesNoUser()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.PostJson("/webapi/setup", new { username = "admin", password = SatelliteWebHost.AdminPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(String.IsNullOrEmpty(host.Configuration["user"]));
            Assert.True(String.IsNullOrEmpty(host.Configuration["password"]));
        }

        [Theory]
        [InlineData("wrong-token")]
        [InlineData("")]
        public async Task Setup_WithWrongToken_Returns401(string token)
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.PostJson("/webapi/setup", new { username = "admin", password = SatelliteWebHost.AdminPassword, setupToken = token });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(String.IsNullOrEmpty(host.Configuration["user"]));
        }

        [Fact]
        public async Task Setup_WithTheLoggedToken_CreatesTheUserWithAHashedPassword()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.Setup();

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(SatelliteWebHost.AdminUser, host.Configuration["user"]);
            Assert.StartsWith(UserAuthData.HashPrefix, host.Configuration["password"]);
            Assert.DoesNotContain(SatelliteWebHost.AdminPassword, host.Configuration["password"]);
            Assert.Contains("\"ready\":true", await (await host.Get("/webapi/ready")).Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Setup_TokenWithSurroundingWhitespace_IsAccepted()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.Setup(token: $"  {host.SetupToken}\n");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Setup_Twice_SecondCallIsRejected()
        {
            await using var host = await SatelliteWebHost.StartAsync();
            var token = host.SetupToken;
            Assert.Equal(HttpStatusCode.NoContent, (await host.Setup(token: token)).StatusCode);

            var second = await host.Setup("attacker", "attacker-password-1", token);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal(SatelliteWebHost.AdminUser, host.Configuration["user"]);
        }

        [Fact]
        public async Task Setup_WhenAUserExists_IsRejectedAndNoTokenIsLogged()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.PostJson("/webapi/setup", new { username = "attacker", password = "attacker-password-1", setupToken = "x" });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Null(host.SetupToken);
            Assert.Equal(SatelliteWebHost.AdminUser, host.Configuration["user"]);
        }

        [Fact]
        public async Task Setup_ShortPassword_IsRejected()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.Setup(password: "short-pw1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(String.IsNullOrEmpty(host.Configuration["user"]));
        }

        [Theory]
        [InlineData(null, "a-long-password")]
        [InlineData("admin", null)]
        [InlineData("", "a-long-password")]
        public async Task Setup_MissingUserOrPassword_IsRejected(string user, string password)
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var response = await host.PostJson("/webapi/setup", new { username = user, password, setupToken = host.SetupToken });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Setup_AfterAWrongToken_StillWorksWithTheRightOne()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Setup(token: "guess")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Setup()).StatusCode);
        }

        [Fact]
        public async Task Setup_IsRateLimited()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            var codes = new List<HttpStatusCode>();
            for (var i = 0; i < 6; i++)
            {
                codes.Add((await host.Setup(token: $"guess-{i}")).StatusCode);
            }

            Assert.Equal(Enumerable.Repeat(HttpStatusCode.Unauthorized, 5), codes.Take(5));
            Assert.Equal(HttpStatusCode.TooManyRequests, codes[5]);
            // even the right token is refused now
            Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Setup()).StatusCode);
        }

        // ---- login

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsAJwtForTheSatellite()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.Login();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync()));
            Assert.Equal("automatica-satellite", jwt.Issuer);
            Assert.Contains("automatica-satellite", jwt.Audiences);
            Assert.Contains(jwt.Claims, c => c.Value == "admin" && c.Type.EndsWith("role"));
        }

        [Theory]
        [InlineData("admin", "wrong-password-1")]
        [InlineData("other", SatelliteWebHost.AdminPassword)]
        [InlineData("ADMIN", SatelliteWebHost.AdminPassword)]
        [InlineData("admin", "")]
        public async Task Login_WrongCredentials_Returns400(string user, string password)
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.Login(user, password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_BeforeSetup_Returns400()
        {
            await using var host = await SatelliteWebHost.StartAsync();

            Assert.Equal(HttpStatusCode.BadRequest, (await host.Login()).StatusCode);
        }

        [Fact]
        public async Task Login_LegacyHash_IsUpgradedOnSuccess()
        {
            var salt = new byte[16];
            var legacy = Convert.ToBase64String(KeyDerivation.Pbkdf2(SatelliteWebHost.AdminPassword, salt, KeyDerivationPrf.HMACSHA1, 10000, 32));
            await using var host = await SatelliteWebHost.StartAsync(new Dictionary<string, string>
            {
                { "user", SatelliteWebHost.AdminUser }, { "password", legacy }, { "salt", Convert.ToBase64String(salt) }
            });

            var response = await host.Login();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith(UserAuthData.HashPrefix, host.Configuration["password"]);
            Assert.True(String.IsNullOrEmpty(host.Configuration["salt"]));
            // the new hash still works
            Assert.Equal(HttpStatusCode.OK, (await host.Login()).StatusCode);
        }

        [Fact]
        public async Task Login_IsRateLimited()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(HttpStatusCode.BadRequest, (await host.Login("admin", $"wrong-password-{i}")).StatusCode);
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Login()).StatusCode);
        }

        // ---- authorization of the api

        [Theory]
        [InlineData("/webapi/config")]
        [InlineData("/webapi/status")]
        [InlineData("/webapi/user")]
        public async Task ProtectedGet_WithoutToken_Returns401(string path)
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Get(path)).StatusCode);
        }

        [Fact]
        public async Task PostConfig_WithoutToken_Returns401AndChangesNothing()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.PostJson("/webapi/config", new Config { ServerUrl = "evil.example", ClientId = "x", ClientKey = "0123456789abcdef-x" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("", host.Configuration["server:master"]);
        }

        [Fact]
        public async Task ProtectedGet_WithLoginToken_Works()
        {
            await using var host = await SatelliteWebHost.StartAsync();
            var token = await host.SetupAndLogin();

            var user = await host.Get("/webapi/user", token);

            Assert.Equal(HttpStatusCode.OK, user.StatusCode);
            Assert.Contains("\"user\":\"admin\"", await user.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Token_SignedWithAnotherKey_Returns401()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());
            // the old satellite signed with a key derived from public data; any other key must fail
            var token = Token(Encoding.UTF8.GetBytes("a-key-that-is-not-the-satellite-key-0123456789"));

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Get("/webapi/config", token)).StatusCode);
        }

        [Fact]
        public async Task Token_SignedWithTheSatelliteKey_Works()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            Assert.Equal(HttpStatusCode.OK, (await host.Get("/webapi/config", Token(host.JwtKey))).StatusCode);
        }

        [Theory]
        [InlineData("someone-else", "automatica-satellite")]
        [InlineData("automatica-satellite", "someone-else")]
        public async Task Token_WrongIssuerOrAudience_Returns401(string issuer, string audience)
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Get("/webapi/config", Token(host.JwtKey, issuer, audience))).StatusCode);
        }

        [Fact]
        public async Task Token_Expired_Returns401()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());
            var token = Token(host.JwtKey, notBefore: new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc), expires: new DateTime(2001, 1, 2, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Get("/webapi/config", token)).StatusCode);
        }

        [Fact]
        public async Task Token_Unsigned_Returns401()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());
            var header = Base64Url(@"{""alg"":""none"",""typ"":""JWT""}");
            var payload = Base64Url($@"{{""role"":""admin"",""iss"":""automatica-satellite"",""aud"":""automatica-satellite"",""exp"":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}");

            Assert.Equal(HttpStatusCode.Unauthorized, (await host.Get("/webapi/config", $"{header}.{payload}.")).StatusCode);
        }

        private static string Base64Url(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        [Fact]
        public async Task JwtKey_IsRandomPerInstallAndPersisted()
        {
            byte[] first;
            await using (var host = await SatelliteWebHost.StartAsync())
            {
                first = host.JwtKey;
                Assert.Equal(32, first.Length);
            }
            await using (var host = await SatelliteWebHost.StartAsync())
            {
                // a fresh config directory is a new install
                Assert.NotEqual(first, host.JwtKey);
            }
        }

        // ---- config

        [Fact]
        public async Task GetConfig_NeverReturnsTheClientKey()
        {
            await using var host = await SatelliteWebHost.StartAsync(new Dictionary<string, string>(SetUpUser())
            {
                { "server:master", "core.local" }, { "server:clientId", "sat1" }, { "server:clientKey", TestKeys.SatelliteKey },
                { "server:port", "5010" }, { "server:useTls", "true" }, { "server:tlsPort", "8884" }, { "server:tlsFingerprint", new string('a', 64) }
            });
            var token = Token(host.JwtKey);

            var response = await host.Get("/webapi/config", token);
            var body = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize<Config>(body, Json);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(TestKeys.SatelliteKey, body);
            Assert.Null(config.ClientKey);
            Assert.True(config.HasClientKey);
            Assert.Equal("core.local", config.ServerUrl);
            Assert.Equal("sat1", config.ClientId);
            Assert.Equal(5010, config.Port);
            Assert.True(config.UseTls);
            Assert.Equal(8884, config.TlsPort);
            Assert.Equal(new string('a', 64), config.TlsFingerprint);
        }

        [Fact]
        public async Task GetConfig_Unconfigured_ReportsNoKeyAndDefaults()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var config = JsonSerializer.Deserialize<Config>(await (await host.Get("/webapi/config", Token(host.JwtKey))).Content.ReadAsStringAsync(), Json);

            Assert.False(config.HasClientKey);
            Assert.Null(config.UseTls);
            Assert.Equal(8883, config.TlsPort);
        }

        [Fact]
        public async Task PostConfig_ShortClientKey_IsRejected()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.PostJson("/webapi/config", new Config { ServerUrl = "127.0.0.1", ClientId = "sat-web", ClientKey = "too-short" }, Token(host.JwtKey));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("", host.Configuration["server:clientKey"]);
            Assert.Equal("", host.Configuration["server:master"]);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("zz:zz")]
        public async Task PostConfig_InvalidFingerprint_IsRejected(string fingerprint)
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            var response = await host.PostJson("/webapi/config", new Config { ServerUrl = "127.0.0.1", ClientId = "sat-web", TlsFingerprint = fingerprint }, Token(host.JwtKey));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("", host.Configuration["server:master"]);
        }

        [Fact]
        public async Task PostConfig_Valid_StoresItAndTheSatelliteConnects()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());
            var token = Token(host.JwtKey);

            var response = await host.PostJson("/webapi/config", new Config
            {
                ServerUrl = "127.0.0.1",
                ClientId = "sat-web",
                ClientKey = TestKeys.SatelliteKey,
                DockerTag = "1.2",
                Port = 5010,
                TlsFingerprint = "AB:" + String.Join(":", Enumerable.Repeat("CD", 31))
            }, token);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("127.0.0.1", host.Configuration["server:master"]);
            Assert.Equal("sat-web", host.Configuration["server:clientId"]);
            Assert.Equal(TestKeys.SatelliteKey, host.Configuration["server:clientKey"]);
            Assert.Equal("1.2", host.Configuration["server:dockerTag"]);
            Assert.Equal("ab" + String.Concat(Enumerable.Repeat("cd", 31)), host.Configuration["server:tlsFingerprint"]);
            Assert.Equal("8883", host.Configuration["server:tlsPort"]);
            Assert.True(String.IsNullOrEmpty(host.Configuration["server:useTls"]));

            // the restart happens inside the request: the satellite is connected to the broker now
            var connect = Assert.Single(host.Broker.Connects);
            Assert.Equal("sat-web", connect.UserName);
            Assert.Equal(TestKeys.SatelliteKey, connect.Password);
            Assert.Contains("\"connected\":true", await (await host.Get("/webapi/status", token)).Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PostConfig_EmptyClientKey_KeepsTheCurrentKey()
        {
            await using var host = await SatelliteWebHost.StartAsync(new Dictionary<string, string>(SetUpUser()) { { "server:clientKey", TestKeys.SatelliteKey } });

            var response = await host.PostJson("/webapi/config", new Config { ServerUrl = "127.0.0.1", ClientId = "sat-web", ClientKey = "" }, Token(host.JwtKey));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(TestKeys.SatelliteKey, host.Configuration["server:clientKey"]);
        }

        [Fact]
        public async Task PostConfig_UseTlsAndPort_AreStored()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            // no fingerprint and TLS on: the satellite would pin on first use; the port has no listener, so it stays unconnected
            var response = await host.PostJson("/webapi/config", new Config { ServerUrl = "", ClientId = "sat-web", UseTls = false, TlsPort = 0 }, Token(host.JwtKey));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("false", host.Configuration["server:useTls"]);
            Assert.Equal("8883", host.Configuration["server:tlsPort"]);
        }

        // ---- status

        [Fact]
        public async Task Status_RunningContainers_DoNotLeakTheirCredentials()
        {
            await using var host = await SatelliteWebHost.StartAsync(new Dictionary<string, string>(SetUpUser())
            {
                { "server:master", "127.0.0.1" }, { "server:clientId", "sat-web" }, { "server:clientKey", TestKeys.SatelliteKey }
            });
            await Eventually.Assert(() => host.Runtime.Connected, "satellite did not connect");
            await host.Runtime.ExecuteAction(new ActionRequest
            {
                Action = SlaveAction.Start, ImageName = "automaticacore/x", Tag = "1", Id = Guid.NewGuid(), ClientKey = TestKeys.PluginKey
            });

            var body = await (await host.Get("/webapi/status", Token(host.JwtKey))).Content.ReadAsStringAsync();

            Assert.Contains("\"connected\":true", body);
            Assert.Contains("\"containerStarted\":true", body);
            Assert.Contains("automaticacore/x:latest-develop", body);
            Assert.DoesNotContain(TestKeys.PluginKey, body);
            Assert.DoesNotContain(TestKeys.SatelliteKey, body);
            Assert.DoesNotContain("AUTOMATICA_SLAVE_PASSWORD", body);
        }

        [Fact]
        public async Task UnknownWebApiPath_Is404()
        {
            await using var host = await SatelliteWebHost.StartAsync(SetUpUser());

            Assert.Equal(HttpStatusCode.NotFound, (await host.Get("/webapi/does-not-exist")).StatusCode);
        }
    }
}
