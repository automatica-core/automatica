using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.WebApi.Authentication;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Tests.Authentication
{
    public class PasswordPolicyTests
    {
        [Theory]
        [InlineData("short", "PASSWORD_TOO_SHORT")]
        [InlineData("", "PASSWORD_TOO_SHORT")]
        [InlineData("administrator", "PASSWORD_EQUALS_USERNAME")]
        [InlineData("password", "PASSWORD_TOO_WEAK")]
        [InlineData("aaaaaaaaaa", "PASSWORD_TOO_WEAK")]
        [InlineData("Correct-Horse-7", null)]
        public void Validate(string password, string expected)
        {
            Assert.Equal(expected, PasswordPolicy.Validate(password, "administrator"));
        }

        [Fact]
        public void Validate_TooLong()
        {
            Assert.Equal("PASSWORD_TOO_LONG", PasswordPolicy.Validate(new string('x', 200) + new string('y', 100), "sa"));
        }

        [Theory]
        [InlineData("sa", "sa", true)]
        [InlineData("visu", "VISU", true)]
        [InlineData("sa", "something-else", false)]
        [InlineData("sa", "", false)]
        public void IsDefaultPassword(string userName, string password, bool expected)
        {
            Assert.Equal(expected, PasswordPolicy.IsDefaultPassword(userName, password));
        }

        [Fact]
        public void GenerateRandomPassword_PassesThePolicy()
        {
            var a = UserHelper.GenerateRandomPassword();
            var b = UserHelper.GenerateRandomPassword();
            Assert.NotEqual(a, b);
            Assert.Equal(20, a.Length);
            Assert.Null(PasswordPolicy.Validate(a, "sa"));
        }
    }

    public class LoginThrottleTests
    {
        private DateTime _now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void LocksAfterFreeAttempts_AndDoubles()
        {
            var throttle = new LoginThrottle(() => _now);

            for (var i = 0; i < LoginThrottle.FreeAttempts - 1; i++)
            {
                throttle.Failed("sa");
                Assert.False(throttle.IsLocked("sa", out _));
            }

            throttle.Failed("SA ");
            Assert.True(throttle.IsLocked("sa", out var retryAfter));
            Assert.Equal(TimeSpan.FromSeconds(30), retryAfter);

            _now = _now.AddSeconds(31);
            Assert.False(throttle.IsLocked("sa", out _));

            throttle.Failed("sa");
            Assert.True(throttle.IsLocked("sa", out retryAfter));
            Assert.Equal(TimeSpan.FromSeconds(60), retryAfter);

            // other user names are not affected
            Assert.False(throttle.IsLocked("visu", out _));
        }

        [Fact]
        public void LockoutIsCapped_AndResetBySuccess()
        {
            var throttle = new LoginThrottle(() => _now);
            for (var i = 0; i < 30; i++)
            {
                throttle.Failed("sa");
            }

            Assert.True(throttle.IsLocked("sa", out var retryAfter));
            Assert.Equal(LoginThrottle.MaxLockout, retryAfter);

            throttle.Succeeded("sa");
            Assert.False(throttle.IsLocked("sa", out _));
        }

        [Fact]
        public void OldFailuresAreForgotten()
        {
            var throttle = new LoginThrottle(() => _now);
            for (var i = 0; i < LoginThrottle.FreeAttempts - 1; i++)
            {
                throttle.Failed("sa");
            }

            _now = _now.Add(LoginThrottle.FailureWindow).AddMinutes(1);
            throttle.Failed("sa");
            Assert.False(throttle.IsLocked("sa", out _));
        }

        private static HttpContext Context(string remote, string forwardedFor)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
            if (forwardedFor != null)
            {
                context.Request.Headers["X-Forwarded-For"] = forwardedFor;
            }

            return context;
        }

        [Fact]
        public void ClientKey_ForwardedForOnlyFromLoopback()
        {
            // through the local frp tunnel: the address the proxy added (the last one), the client's own entries are ignored
            Assert.Equal("fwd:203.0.113.7", LoginThrottle.GetClientKey(Context("127.0.0.1", "1.2.3.4, 203.0.113.7")));
            Assert.Equal("fwd:203.0.113.7", LoginThrottle.GetClientKey(Context("::1", "203.0.113.7")));

            // a client in the network cannot choose its rate limit partition
            Assert.Equal("192.168.1.20", LoginThrottle.GetClientKey(Context("192.168.1.20", "203.0.113.7")));
            Assert.Equal("127.0.0.1", LoginThrottle.GetClientKey(Context("127.0.0.1", "not-an-address")));
            Assert.Equal("127.0.0.1", LoginThrottle.GetClientKey(Context("127.0.0.1", null)));
        }

        [Fact]
        public void SanitizeForLog_RemovesControlCharacters()
        {
            Assert.Equal("sa_[INF] fake", LoginThrottle.SanitizeForLog("sa\n[INF] fake"));
            Assert.Equal(new string('a', 64) + "...", LoginThrottle.SanitizeForLog(new string('a', 100)));
            Assert.Equal("<null>", LoginThrottle.SanitizeForLog(null));
        }
    }

    public class UserClaimsTests
    {
        private static UserClaimsProvider.UserClaims Claims(string stamp, bool mustChange = false) =>
            new(Guid.NewGuid(), new[] { "Administrator" }, new[] { Guid.NewGuid() }, Array.Empty<Model.Models.User.User2Role>(), stamp, mustChange);

        [Fact]
        public void Stamp_MustMatch()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(UserClaimsProvider.SecurityStampClaimType, "abc") }, "test");
            Assert.True(UserClaimsProvider.IsStampValid(identity, Claims("abc")));
            Assert.False(UserClaimsProvider.IsStampValid(identity, Claims("abd")));
            Assert.False(UserClaimsProvider.IsStampValid(identity, Claims(null)));

            // tokens issued before the security stamp existed are rejected
            Assert.False(UserClaimsProvider.IsStampValid(new ClaimsIdentity(Array.Empty<Claim>(), "test"), Claims("abc")));
        }

        [Fact]
        public void Apply_MustChangePassword_RemovesAllRoles()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Administrator") }, "test");
            UserClaimsProvider.Apply(identity, Claims("abc", mustChange: true));

            Assert.False(identity.HasClaim(a => a.Type == ClaimTypes.Role));
            Assert.True(identity.HasClaim(a => a.Type == UserClaimsProvider.MustChangePasswordClaimType));

            UserClaimsProvider.Apply(identity, Claims("abc"));
            Assert.True(identity.HasClaim(ClaimTypes.Role, "Administrator"));
            Assert.False(identity.HasClaim(a => a.Type == UserClaimsProvider.MustChangePasswordClaimType));
        }
    }

    public class LogRedactionTests
    {
        [Fact]
        public void AccessTokenAndBearer_AreRedacted()
        {
            Assert.Equal("GET /signalr/dataHub?id=1&access_token=***&x=2",
                Automatica.Core.Logging.LogRedaction.Redact("GET /signalr/dataHub?id=1&access_token=eyJhbGciOi.eyJzdWIi.sig&x=2"));
            Assert.Equal("Authorization: Bearer ***", Automatica.Core.Logging.LogRedaction.Redact("Authorization: Bearer eyJhbGciOi.eyJzdWIi.c2ln"));
            const string plain = "nothing to hide";
            Assert.Same(plain, Automatica.Core.Logging.LogRedaction.Redact(plain));
        }
    }

    public class LoginFlowTests : BaseControllerTest<AuthenticationController>
    {
        private const string NewPassword = "Correct-Horse-7";

        private async Task SetPassword(string userName, string password, bool mustChange)
        {
            await using var db = new AutomaticaContext(Configuration);
            var user = await db.Users.SingleAsync(a => a.UserName == userName);
            user.Salt = UserHelper.GenerateNewSalt();
            user.Password = UserHelper.CreatePasswordHash(password, user.Salt);
            user.MustChangePassword = mustChange;
            await db.SaveChangesAsync();
        }

        private async Task<User> LoadUser(string userName)
        {
            await using var db = new AutomaticaContext(Configuration);
            return await db.Users.AsNoTracking().SingleAsync(a => a.UserName == userName);
        }

        private static JwtSecurityToken Read(User user) => new JwtSecurityTokenHandler().ReadJwtToken(user.Token);

        private AccountController CreateAccountController(User loggedIn)
        {
            var services = new ServiceCollection().AddSingleton(new LoginThrottle()).BuildServiceProvider();
            var controller = new AccountController(new AutomaticaContext(Configuration), NullLogger<AccountController>.Instance,
                ServiceProvider.GetRequiredService<IUserCache>());
            var token = Read(loggedIn);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services,
                    User = new ClaimsPrincipal(new ClaimsIdentity(token.Claims.Select(a =>
                        a.Type == "nameid" ? new Claim(UserClaimsProvider.UserIdClaimType, a.Value) : a), "test"))
                }
            };
            return controller;
        }

        [Fact]
        public async Task NewInstall_HasNoDefaultCredentials()
        {
            var result = await Controller.Login(new UserAuthData { Username = "sa", Password = "sa" });
            Assert.IsType<UnauthorizedResult>(result.Result);

            result = await Controller.Login(new UserAuthData { Username = "visu", Password = "visu" });
            Assert.IsType<UnauthorizedResult>(result.Result);

            var sa = await LoadUser("sa");
            Assert.True(sa.MustChangePassword);
            Assert.False(string.IsNullOrEmpty(sa.SecurityStamp));
        }

        [Fact]
        public async Task DefaultPassword_ForcesPasswordChange()
        {
            // an older install that still uses sa/sa
            await SetPassword("sa", "sa", false);

            var result = await Controller.Login(new UserAuthData { Username = "sa", Password = "sa" });
            var user = result.Value;
            Assert.NotNull(user);
            Assert.True(user.MustChangePassword);
            Assert.Empty(user.InverseThis2Roles);
            Assert.Null(user.SecurityStamp);

            var token = Read(user);
            Assert.Contains(token.Claims, a => a.Type == UserClaimsProvider.MustChangePasswordClaimType);
            Assert.DoesNotContain(token.Claims, a => a.Type == "role");

            Assert.True((await LoadUser("sa")).MustChangePassword);
            var oldStamp = (await LoadUser("sa")).SecurityStamp;

            var account = CreateAccountController(user);

            var weak = await account.ChangePassword(new ChangePasswordData { OldPassword = "sa", NewPassword = "sa" });
            Assert.IsType<BadRequestObjectResult>(weak.Result);

            var wrongOld = await account.ChangePassword(new ChangePasswordData { OldPassword = "wrong", NewPassword = NewPassword });
            Assert.IsType<BadRequestObjectResult>(wrongOld.Result);

            var changed = await account.ChangePassword(new ChangePasswordData { OldPassword = "sa", NewPassword = NewPassword });
            Assert.NotNull(changed.Value);
            Assert.False(changed.Value.MustChangePassword);
            var newToken = Read(changed.Value);
            Assert.DoesNotContain(newToken.Claims, a => a.Type == UserClaimsProvider.MustChangePasswordClaimType);
            Assert.Contains(newToken.Claims, a => a.Type == "role");

            var stored = await LoadUser("sa");
            Assert.False(stored.MustChangePassword);
            Assert.NotEqual(oldStamp, stored.SecurityStamp);

            // the old password is gone, the new one works without a forced change
            Assert.IsType<UnauthorizedResult>((await Controller.Login(new UserAuthData { Username = "sa", Password = "sa" })).Result);
            var again = await Controller.Login(new UserAuthData { Username = "sa", Password = NewPassword });
            Assert.False(again.Value.MustChangePassword);
        }

        [Fact]
        public async Task FailedLogins_LockTheUserName()
        {
            await SetPassword("sa", NewPassword, false);
            var throttle = new LoginThrottle();
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = new ServiceCollection().AddSingleton(throttle).BuildServiceProvider()
                }
            };

            try
            {
                for (var i = 0; i < LoginThrottle.FreeAttempts; i++)
                {
                    Assert.IsType<UnauthorizedResult>((await Controller.Login(new UserAuthData { Username = "sa", Password = "wrong" + i })).Result);
                }

                // even the right password is refused while locked
                var locked = await Controller.Login(new UserAuthData { Username = "sa", Password = NewPassword });
                var status = Assert.IsType<StatusCodeResult>(locked.Result);
                Assert.Equal(StatusCodes.Status429TooManyRequests, status.StatusCode);
                Assert.True(Controller.Response.Headers.ContainsKey("Retry-After"));
            }
            finally
            {
                Controller.ControllerContext = new ControllerContext();
            }
        }
    }
}
