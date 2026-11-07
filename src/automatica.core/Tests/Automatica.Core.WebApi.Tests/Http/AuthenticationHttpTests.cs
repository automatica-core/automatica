using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Authentication;
using Automatica.Core.WebApi.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// Login, lockout, rate limit, forced password change and token validation over HTTP.
    /// Every test uses its own user names (lockout is per user name) and its own client address (rate limit per address).
    /// </summary>
    public class AuthenticationHttpTests : IClassFixture<WebApiHostFixture>
    {
        private static int _clientCounter;
        private readonly WebApiHost _host;

        public AuthenticationHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        /// <summary>
        /// A client with its own rate limit partition (the test server is loopback, X-Forwarded-For is honoured)
        /// </summary>
        private HttpClient Client(string token = null)
        {
            var n = Interlocked.Increment(ref _clientCounter);
            return _host.CreateClient(token, forwardedFor: $"198.51.100.{n % 250 + 1}, 203.0.{n / 250}.{n % 250 + 1}");
        }

        private static Task<HttpResponseMessage> Login(HttpClient client, string userName, string password) =>
            client.PostJson("webapi/auth/login", new UserAuthData { Username = userName, Password = password });

        private static Task<HttpResponseMessage> ChangePassword(HttpClient client, string oldPassword, string newPassword) =>
            client.PostJson("webapi/account/password", new ChangePasswordData { OldPassword = oldPassword, NewPassword = newPassword });

        private static JwtSecurityToken ReadToken(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

        private Guid NewUser(out string userName, string password = WebApiHost.StrongPassword, bool mustChange = false, string role = Role.AdminRole)
        {
            userName = _host.UniqueName("login");
            return _host.CreateUser(userName, password, new[] { role }, mustChangePassword: mustChange);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithWorkingToken()
        {
            NewUser(out var userName);
            var client = Client();

            var response = await Login(client, userName, WebApiHost.StrongPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal(userName, (string)body["userName"]);
            Assert.False((bool)body["mustChangePassword"]);
            var token = (string)body["token"];
            Assert.False(string.IsNullOrEmpty(token));

            var ping = await _host.CreateClient(token).GetAsync("webapi/Ping");
            Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
        }

        [Fact]
        public async Task Login_Response_ContainsNoPasswordHashSaltOrStamp()
        {
            NewUser(out var userName);
            var response = await Login(Client(), userName, WebApiHost.StrongPassword);
            var body = (JObject)await response.ReadJson();

            Assert.True(string.IsNullOrEmpty((string)body["password"]));
            Assert.True(body["passwordConfirm"] == null || body["passwordConfirm"].Type == JTokenType.Null);
            Assert.Null(body["salt"]);
            Assert.Null(body["securityStamp"]);
        }

        [Fact]
        public async Task Login_Token_CarriesUserIdStampAndRoles()
        {
            var userId = NewUser(out var userName);
            var body = await (await Login(Client(), userName, WebApiHost.StrongPassword)).ReadJson();
            var token = ReadToken((string)body["token"]);

            Assert.Contains(token.Claims, a => a.Type == "nameid" && a.Value == userId.ToString());
            Assert.Contains(token.Claims, a => a.Type == "role" && a.Value == Role.AdminRole);
            Assert.Contains(token.Claims, a => a.Type == UserClaimsProvider.SecurityStampClaimType && !string.IsNullOrEmpty(a.Value));
            Assert.DoesNotContain(token.Claims, a => a.Type == UserClaimsProvider.MustChangePasswordClaimType);
            // a week, never unlimited
            Assert.InRange(token.ValidTo - token.ValidFrom, TimeSpan.FromDays(6.9), TokenIssuer.TokenLifetime);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            NewUser(out var userName);
            var response = await Login(Client(), userName, "Wrong-Horse-8");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_UnknownUser_Returns401()
        {
            var response = await Login(Client(), _host.UniqueName("nobody"), WebApiHost.StrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_UserNameIsCaseSensitive()
        {
            NewUser(out var userName);
            var response = await Login(Client(), userName.ToUpperInvariant(), WebApiHost.StrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"username\":\"\",\"password\":\"x\"}")]
        [InlineData("{\"username\":\"sa\"}")]
        public async Task Login_IncompleteBody_Returns401(string json)
        {
            var response = await Client().PostAsync("webapi/auth/login", HttpTestExtensions.RawJson(json));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_OverlongUserNameOrPassword_Returns401()
        {
            var client = Client();
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, new string('u', 129), WebApiHost.StrongPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "sa", new string('p', 257))).StatusCode);
        }

        [Fact]
        public async Task Login_SeededAccounts_HaveNoDefaultPasswords()
        {
            var client = Client();
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "sa", "sa")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "visu", "visu")).StatusCode);
        }

        [Fact]
        public async Task Login_AfterFreeAttempts_Returns429WithRetryAfter_EvenForTheRightPassword()
        {
            NewUser(out var userName);
            var client = Client();

            for (var i = 0; i < LoginThrottle.FreeAttempts; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, userName, "Wrong-Horse-" + i)).StatusCode);
            }

            var locked = await Login(client, userName, WebApiHost.StrongPassword);
            Assert.Equal((HttpStatusCode)429, locked.StatusCode);
            var retryAfter = Assert.Single(locked.Headers.GetValues("Retry-After"));
            Assert.InRange(int.Parse(retryAfter), 1, 30);
        }

        [Fact]
        public async Task Login_Lockout_IsPerUserName_AndIgnoresCaseAndWhitespace()
        {
            NewUser(out var lockedUser);
            NewUser(out var otherUser);

            for (var i = 0; i < LoginThrottle.FreeAttempts; i++)
            {
                // from different addresses: the lockout follows the user name, not the client
                await Login(Client(), i % 2 == 0 ? lockedUser : " " + lockedUser.ToUpperInvariant(), "Wrong-Horse-" + i);
            }

            Assert.Equal((HttpStatusCode)429, (await Login(Client(), lockedUser, WebApiHost.StrongPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Login(Client(), otherUser, WebApiHost.StrongPassword)).StatusCode);
        }

        [Fact]
        public async Task Login_Success_ResetsTheFailureCount()
        {
            NewUser(out var userName);
            var client = Client();

            for (var i = 0; i < LoginThrottle.FreeAttempts - 1; i++)
            {
                await Login(client, userName, "Wrong-Horse-" + i);
            }
            Assert.Equal(HttpStatusCode.OK, (await Login(client, userName, WebApiHost.StrongPassword)).StatusCode);

            client = Client();
            for (var i = 0; i < LoginThrottle.FreeAttempts - 1; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, userName, "Wrong-Horse-" + i)).StatusCode);
            }
            Assert.Equal(HttpStatusCode.OK, (await Login(client, userName, WebApiHost.StrongPassword)).StatusCode);
        }

        [Fact]
        public async Task Login_UnknownUserNames_AreLockedAsWell()
        {
            var userName = _host.UniqueName("ghost");
            var client = Client();
            for (var i = 0; i < LoginThrottle.FreeAttempts; i++)
            {
                await Login(client, userName, "Wrong-Horse-" + i);
            }

            Assert.Equal((HttpStatusCode)429, (await Login(Client(), userName, "Wrong-Horse-x")).StatusCode);
        }

        [Fact]
        public async Task Login_RateLimit_RejectsThe11thRequestOfAClient()
        {
            var client = _host.CreateClient(forwardedFor: "203.0.113.201");

            for (var i = 0; i < 10; i++)
            {
                // a different (unknown) user name every time, so the user lockout does not kick in
                Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, _host.UniqueName("rate"), "Wrong-Horse-1")).StatusCode);
            }

            var limited = await Login(client, _host.UniqueName("rate"), "Wrong-Horse-1");
            Assert.Equal((HttpStatusCode)429, limited.StatusCode);
            Assert.False(limited.Headers.Contains("Retry-After"));

            // another client (through the local proxy) has its own partition
            var other = _host.CreateClient(forwardedFor: "203.0.113.202");
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(other, _host.UniqueName("rate"), "Wrong-Horse-1")).StatusCode);
        }

        [Fact]
        public async Task Login_RateLimit_ForwardedForOfANonLoopbackClientIsIgnored()
        {
            for (var i = 0; i < 10; i++)
            {
                // a client in the network rotates X-Forwarded-For: still one partition (its own address)
                var client = _host.CreateClient(forwardedFor: $"192.0.2.{i + 1}", remoteIp: "10.1.2.3");
                Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, _host.UniqueName("spoof"), "Wrong-Horse-1")).StatusCode);
            }

            var limited = await Login(_host.CreateClient(forwardedFor: "192.0.2.99", remoteIp: "10.1.2.3"), _host.UniqueName("spoof"), "Wrong-Horse-1");
            Assert.Equal((HttpStatusCode)429, limited.StatusCode);
        }

        [Fact]
        public async Task Login_DefaultPasswordOfOldInstall_ReturnsPendingToken()
        {
            // an old install: user name == password
            var userName = _host.UniqueName("legacy");
            _host.CreateUser(userName, userName, new[] { Role.AdminRole });

            var response = await Login(Client(), userName, userName);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadJson();
            Assert.True((bool)body["mustChangePassword"]);
            Assert.Empty((JArray)body["inverseThis2Roles"]);
            var token = ReadToken((string)body["token"]);
            Assert.Contains(token.Claims, a => a.Type == UserClaimsProvider.MustChangePasswordClaimType && a.Value == "true");
            Assert.DoesNotContain(token.Claims, a => a.Type == "role");

            await using var db = _host.CreateDbContext();
            Assert.True((await db.Users.AsNoTracking().SingleAsync(a => a.UserName == userName)).MustChangePassword);
        }

        [Fact]
        public async Task Login_UserFlaggedByAdmin_ReturnsPendingToken()
        {
            NewUser(out var userName, mustChange: true);

            var body = await (await Login(Client(), userName, WebApiHost.StrongPassword)).ReadJson();

            Assert.True((bool)body["mustChangePassword"]);
            Assert.Contains(ReadToken((string)body["token"]).Claims, a => a.Type == UserClaimsProvider.MustChangePasswordClaimType);
        }

        [Theory]
        [InlineData("GET", "webapi/Ping")]
        [InlineData("GET", "webapi/usermgm/users")]
        [InlineData("GET", "webapi/settings")]
        [InlineData("GET", "webapi/areas")]
        [InlineData("GET", "webapi/visualization/pages")]
        [InlineData("POST", "webapi/auth/logout")]
        public async Task PendingToken_IsRejectedEverywhereButThePasswordChange(string method, string url)
        {
            var token = await PendingToken();
            var response = await _host.CreateClient(token).SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = HttpTestExtensions.RawJson("{}") });

            if (url == "webapi/auth/logout")
            {
                // anonymous endpoint
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        private async Task<string> PendingToken()
        {
            var userName = _host.UniqueName("pending");
            _host.CreateUser(userName, userName, new[] { Role.AdminRole });
            var body = await (await Login(Client(), userName, userName)).ReadJson();
            return (string)body["token"];
        }

        [Fact]
        public async Task PendingToken_ChangePassword_ReturnsFullTokenAndRevokesThePendingOne()
        {
            var userName = _host.UniqueName("pending");
            _host.CreateUser(userName, userName, new[] { Role.AdminRole });
            var pending = (string)(await (await Login(Client(), userName, userName)).ReadJson())["token"];

            var response = await ChangePassword(Client(pending), userName, WebApiHost.StrongPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadJson();
            Assert.False((bool)body["mustChangePassword"]);
            var newToken = (string)body["token"];
            Assert.Contains(ReadToken(newToken).Claims, a => a.Type == "role" && a.Value == Role.AdminRole);

            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(newToken).GetAsync("webapi/usermgm/users")).StatusCode);
            // the security stamp changed: the pending token is revoked
            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient(pending).GetAsync("webapi/Ping")).StatusCode);

            // the old password is gone, the new one logs in without a forced change
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(Client(), userName, userName)).StatusCode);
            var again = await (await Login(Client(), userName, WebApiHost.StrongPassword)).ReadJson();
            Assert.False((bool)again["mustChangePassword"]);
        }

        [Fact]
        public async Task DefaultPasswordsAllowed_OnDemoServers_NoForcedChange()
        {
            using var demo = new WebApiHost(settings: new Dictionary<string, string> { [AuthenticationController.AllowDefaultPasswordsKey] = "true" });
            var userName = demo.UniqueName("demo");
            demo.CreateUser(userName, userName, new[] { Role.AdminRole });

            var body = await (await Login(demo.CreateClient(), userName, userName)).ReadJson();

            Assert.False((bool)body["mustChangePassword"]);
            Assert.Equal(HttpStatusCode.OK, (await demo.CreateClient((string)body["token"]).GetAsync("webapi/Ping")).StatusCode);
        }

        [Theory]
        [InlineData("Wrong-Horse-8", "Brand-New-Pass-9", "WRONG_PASSWORD")]
        [InlineData(WebApiHost.StrongPassword, WebApiHost.StrongPassword, "PASSWORD_UNCHANGED")]
        [InlineData(WebApiHost.StrongPassword, "short", "PASSWORD_TOO_SHORT")]
        [InlineData(WebApiHost.StrongPassword, "", "PASSWORD_TOO_SHORT")]
        [InlineData(WebApiHost.StrongPassword, "password", "PASSWORD_TOO_WEAK")]
        [InlineData(WebApiHost.StrongPassword, "abababababab", "PASSWORD_TOO_WEAK")]
        public async Task ChangePassword_InvalidInput_Returns400WithErrorKey(string oldPassword, string newPassword, string expectedError)
        {
            var userId = NewUser(out _);
            var response = await ChangePassword(Client(_host.TokenFor(userId)), oldPassword, newPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expectedError, (string)(await response.ReadJson())["error"]);
        }

        [Fact]
        public async Task ChangePassword_EqualsUserName_Returns400()
        {
            var userName = _host.UniqueName("Longusername");
            var userId = _host.CreateUser(userName, WebApiHost.StrongPassword, new[] { Role.AdminRole });

            var response = await ChangePassword(Client(_host.TokenFor(userId)), WebApiHost.StrongPassword, userName.ToLowerInvariant());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("PASSWORD_EQUALS_USERNAME", (string)(await response.ReadJson())["error"]);
        }

        [Fact]
        public async Task ChangePassword_TooLong_Returns400()
        {
            var userId = NewUser(out _);
            var response = await ChangePassword(Client(_host.TokenFor(userId)), WebApiHost.StrongPassword, new string('x', 200) + new string('y', 100));
            Assert.Equal("PASSWORD_TOO_LONG", (string)(await response.ReadJson())["error"]);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"oldPassword\":\"Correct-Horse-7\"}")]
        [InlineData("{\"newPassword\":\"Brand-New-Pass-9\"}")]
        public async Task ChangePassword_IncompleteBody_Returns400(string json)
        {
            var userId = NewUser(out _);
            var response = await Client(_host.TokenFor(userId)).PostAsync("webapi/account/password", HttpTestExtensions.RawJson(json));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithoutToken_Returns401()
        {
            var response = await ChangePassword(Client(), WebApiHost.StrongPassword, "Brand-New-Pass-9");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_ForNonAdminUser_Works()
        {
            var userId = NewUser(out var userName, role: Role.VisuRole);
            var response = await ChangePassword(Client(_host.TokenFor(userId)), WebApiHost.StrongPassword, "Brand-New-Pass-9");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Login(Client(), userName, "Brand-New-Pass-9")).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WrongOldPassword_CountsTowardsTheLockout()
        {
            var userId = NewUser(out var userName);
            var token = _host.TokenFor(userId);

            for (var i = 0; i < LoginThrottle.FreeAttempts; i++)
            {
                var response = await ChangePassword(Client(token), "Wrong-Horse-" + i, "Brand-New-Pass-9");
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            Assert.Equal((HttpStatusCode)429, (await ChangePassword(Client(token), WebApiHost.StrongPassword, "Brand-New-Pass-9")).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await Login(Client(), userName, WebApiHost.StrongPassword)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_Success_RevokesAllOtherTokensOfTheUser()
        {
            var userId = NewUser(out _);
            var first = _host.TokenFor(userId);
            var second = _host.TokenFor(userId);

            var response = await ChangePassword(Client(first), WebApiHost.StrongPassword, "Brand-New-Pass-9");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fresh = (string)(await response.ReadJson())["token"];

            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient(first).GetAsync("webapi/Ping")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient(second).GetAsync("webapi/Ping")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(fresh).GetAsync("webapi/Ping")).StatusCode);
        }

        [Fact]
        public async Task Logout_IsAnonymousAndReturnsTrue()
        {
            var response = await _host.CreateClient().PostAsync("webapi/auth/logout", HttpTestExtensions.RawJson("{}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((bool)await response.ReadJson());
        }
    }

    /// <summary>
    /// Validation of the bearer token on every request (signature, lifetime, user, security stamp, current roles).
    /// </summary>
    public class TokenValidationHttpTests : IClassFixture<WebApiHostFixture>
    {
        private readonly WebApiHost _host;

        public TokenValidationHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        private static string Sign(IEnumerable<Claim> claims, byte[] key, DateTime? notBefore = null, DateTime? expires = null)
        {
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = notBefore,
                IssuedAt = notBefore,
                Expires = expires ?? DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private (Guid id, string name, string stamp) NewAdmin()
        {
            var name = _host.UniqueName("tokenadmin");
            var id = _host.CreateUser(name, WebApiHost.StrongPassword, new[] { Role.AdminRole });
            using var db = _host.CreateDbContext();
            return (id, name, db.Users.AsNoTracking().Single(a => a.ObjId == id).SecurityStamp);
        }

        private static List<Claim> Claims(Guid id, string name, string stamp, bool withRole = true)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, name),
                new(UserClaimsProvider.UserIdClaimType, id.ToString())
            };
            if (stamp != null)
            {
                claims.Add(new Claim(UserClaimsProvider.SecurityStampClaimType, stamp));
            }

            if (withRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, Role.AdminRole));
            }

            return claims;
        }

        private async Task<HttpStatusCode> Ping(string token) => (await _host.CreateClient(token).GetAsync("webapi/usermgm/roles")).StatusCode;

        [Fact]
        public async Task ValidToken_IsAccepted()
        {
            var (id, name, stamp) = NewAdmin();
            Assert.Equal(HttpStatusCode.OK, await Ping(Sign(Claims(id, name, stamp), JwtSigningKey.Get())));
        }

        [Fact]
        public async Task TokenSignedWithAnotherKey_Is401()
        {
            var (id, name, stamp) = NewAdmin();
            var otherKey = Enumerable.Range(0, 32).Select(a => (byte)(a * 7 + 1)).ToArray();
            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(Sign(Claims(id, name, stamp), otherKey)));
        }

        [Fact]
        public async Task TamperedPayload_Is401()
        {
            var userId = _host.CreateUser(_host.UniqueName("visu"), WebApiHost.StrongPassword, new[] { Role.VisuRole });
            var token = _host.TokenFor(userId);
            var parts = token.Split('.');
            // swap in the payload of an admin token, keep the signature of the visu token
            var (id, name, stamp) = NewAdmin();
            var adminParts = Sign(Claims(id, name, stamp), JwtSigningKey.Get()).Split('.');
            var forged = $"{parts[0]}.{adminParts[1]}.{parts[2]}";

            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(forged));
        }

        [Fact]
        public async Task UnsignedToken_Is401()
        {
            var (id, name, stamp) = NewAdmin();
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(), new JwtPayload(Claims(id, name, stamp))));
            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(token));
        }

        [Fact]
        public async Task ExpiredToken_Is401()
        {
            var (id, name, stamp) = NewAdmin();
            var token = Sign(Claims(id, name, stamp), JwtSigningKey.Get(), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 8, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(token));
        }

        [Fact]
        public async Task TokenWithoutSecurityStamp_Is401()
        {
            var (id, name, _) = NewAdmin();
            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(Sign(Claims(id, name, null), JwtSigningKey.Get())));
        }

        [Fact]
        public async Task TokenWithWrongSecurityStamp_Is401()
        {
            var (id, name, _) = NewAdmin();
            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(Sign(Claims(id, name, "not-the-stamp"), JwtSigningKey.Get())));
        }

        [Fact]
        public async Task TokenOfDeletedUser_Is401()
        {
            var (id, name, stamp) = NewAdmin();
            var token = Sign(Claims(id, name, stamp), JwtSigningKey.Get());
            Assert.Equal(HttpStatusCode.OK, await Ping(token));

            await using (var db = _host.CreateDbContext())
            {
                db.Users.Remove(await db.Users.SingleAsync(a => a.ObjId == id));
                await db.SaveChangesAsync();
            }
            _host.InvalidateCaches();

            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(token));
        }

        [Fact]
        public async Task TokenOfRenamedUser_Is401()
        {
            var (id, name, stamp) = NewAdmin();
            var token = Sign(Claims(id, name, stamp), JwtSigningKey.Get());

            await using (var db = _host.CreateDbContext())
            {
                var user = await db.Users.SingleAsync(a => a.ObjId == id);
                user.UserName = name + "-renamed";
                await db.SaveChangesAsync();
            }
            _host.InvalidateCaches();

            Assert.Equal(HttpStatusCode.Unauthorized, await Ping(token));
        }

        [Fact]
        public async Task RoleClaimsInTheToken_AreReplacedByTheCurrentRoles()
        {
            // the token claims admin, the database says visu
            var name = _host.UniqueName("visu");
            var id = _host.CreateUser(name, WebApiHost.StrongPassword, new[] { Role.VisuRole });
            string stamp;
            await using (var db = _host.CreateDbContext())
            {
                stamp = db.Users.AsNoTracking().Single(a => a.ObjId == id).SecurityStamp;
            }

            Assert.Equal(HttpStatusCode.Forbidden, await Ping(Sign(Claims(id, name, stamp), JwtSigningKey.Get())));
        }

        [Fact]
        public async Task RoleRemovedAfterLogin_TakesEffectWithoutNewLogin()
        {
            var (id, _, _) = NewAdmin();
            var token = _host.TokenFor(id);
            Assert.Equal(HttpStatusCode.OK, await Ping(token));

            await using (var db = _host.CreateDbContext())
            {
                db.User2Roles.RemoveRange(db.User2Roles.Where(a => a.This2User == id));
                await db.SaveChangesAsync();
            }
            _host.InvalidateCaches();

            Assert.Equal(HttpStatusCode.Forbidden, await Ping(token));
        }

        [Fact]
        public async Task RoleThroughUserGroup_GrantsAccess_AndGroupRemovalRevokesIt()
        {
            var group = _host.CreateUserGroup(_host.UniqueName("admins"), Role.AdminRole);
            var id = _host.CreateUser(_host.UniqueName("grouped"), WebApiHost.StrongPassword, Array.Empty<string>(), new[] { group });
            var token = _host.TokenFor(id);
            Assert.Equal(HttpStatusCode.OK, await Ping(token));

            await using (var db = _host.CreateDbContext())
            {
                db.User2Groups.RemoveRange(db.User2Groups.Where(a => a.This2User == id));
                await db.SaveChangesAsync();
            }
            _host.InvalidateCaches();

            Assert.Equal(HttpStatusCode.Forbidden, await Ping(token));
        }

        [Fact]
        public async Task MustChangeFlagSetAfterLogin_RestrictsTheExistingToken()
        {
            var (id, _, _) = NewAdmin();
            var token = _host.TokenFor(id);

            await using (var db = _host.CreateDbContext())
            {
                (await db.Users.SingleAsync(a => a.ObjId == id)).MustChangePassword = true;
                await db.SaveChangesAsync();
            }
            _host.InvalidateCaches();

            Assert.Equal(HttpStatusCode.Forbidden, await Ping(token));
        }

        [Theory]
        [InlineData("Bearer")]
        [InlineData("Bearer not-a-jwt")]
        [InlineData("Basic c2E6c2E=")]
        public async Task MalformedAuthorizationHeader_Is401(string header)
        {
            var client = _host.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", header);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("webapi/Ping")).StatusCode);
        }

        [Fact]
        public async Task InvalidToken_OnAnonymousEndpoint_IsIgnored()
        {
            var response = await _host.CreateClient("not-a-jwt").GetAsync("webapi/version");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AccessTokenQueryParameter_IsNotAcceptedForTheWebApi()
        {
            var (id, _, _) = NewAdmin();
            var token = _host.TokenFor(id);
            var response = await _host.CreateClient().GetAsync($"webapi/Ping?access_token={token}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
