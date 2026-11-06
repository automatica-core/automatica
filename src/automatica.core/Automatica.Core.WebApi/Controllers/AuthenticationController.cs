using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Controllers
{
    public class UserAuthData
    {
        public UserAuthData()
        {

        }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    [Route("webapi/auth")]
    [AllowAnonymous]
    public class AuthenticationController : BaseController
    {
        /// <summary>
        /// Name of the rate limiter policy of the login (registered in the startup).
        /// </summary>
        public const string LoginRateLimitPolicy = "login";

        /// <summary>
        /// Verified for unknown user names, so the response time does not tell whether a user exists
        /// </summary>
        private static readonly Lazy<(string salt, string hash)> DummyCredentials = new(() =>
        {
            var salt = UserHelper.GenerateNewSalt();
            return (salt, UserHelper.CreatePasswordHash(UserHelper.GenerateRandomPassword(), salt));
        });

        /// <summary>
        /// Set to true on public demo servers only: the old default credentials (sa/sa) keep working without a password change
        /// </summary>
        public const string AllowDefaultPasswordsKey = "server:allow_default_passwords";

        private readonly ILogger<AuthenticationController> _logger;
        private readonly bool _allowDefaultPasswords;

        public AuthenticationController(AutomaticaContext dbContext, ILogger<AuthenticationController> logger, IConfiguration configuration) : base(dbContext)
        {
            _logger = logger;
            _allowDefaultPasswords = bool.TryParse(configuration?[AllowDefaultPasswordsKey], out var allow) && allow;
        }

        private LoginThrottle Throttle => HttpContext?.RequestServices?.GetService<LoginThrottle>();

        [HttpPost]
        [Route("login")]
        [EnableRateLimiting(LoginRateLimitPolicy)]
        public async Task<ActionResult<User>> Login([FromBody]UserAuthData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Username) || data.Password == null || data.Username.Length > 128 || data.Password.Length > PasswordPolicy.MaxLength)
            {
                return Unauthorized();
            }

            var throttle = Throttle;
            if (throttle != null && throttle.IsLocked(data.Username, out var retryAfter))
            {
                _logger.LogWarning($"Login for user {LoginThrottle.SanitizeForLog(data.Username)} is locked for {retryAfter.TotalSeconds:0}s after failed logins (from {HttpContext?.Connection?.RemoteIpAddress})");
                Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
                return StatusCode(StatusCodes.Status429TooManyRequests);
            }

            // user names should be unique, but older databases may contain duplicates: the password decides
            var candidates = await DbContext.Users.AsNoTracking().Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .Where(a => a.UserName == data.Username).ToListAsync();

            if (candidates.Count == 0)
            {
                // same work as for an existing user
                UserHelper.VerifyPassword(data.Password, DummyCredentials.Value.salt, DummyCredentials.Value.hash, out _);
            }

            foreach (var user in candidates)
            {
                if (!UserHelper.VerifyPassword(data.Password, user.Salt, user.Password, out var needsRehash))
                {
                    continue;
                }

                throttle?.Succeeded(data.Username);

                // the old default credentials (sa/sa, visu/visu) must be replaced before the user can do anything else
                var mustChange = user.MustChangePassword || (!_allowDefaultPasswords && PasswordPolicy.IsDefaultPassword(user.UserName, data.Password));

                if (needsRehash || mustChange != user.MustChangePassword || string.IsNullOrEmpty(user.SecurityStamp))
                {
                    var stamp = await UpdateUser(user.ObjId, needsRehash ? data.Password : null, mustChange);
                    user.SecurityStamp = stamp ?? user.SecurityStamp;
                }
                user.MustChangePassword = mustChange;

                if (mustChange)
                {
                    _logger.LogWarning($"User {LoginThrottle.SanitizeForLog(user.UserName)} must change the password");
                }

                return TokenIssuer.Login(DbContext, user);
            }

            throttle?.Failed(data.Username);
            _logger.LogWarning($"Failed login for user {LoginThrottle.SanitizeForLog(data.Username)} from {HttpContext?.Connection?.RemoteIpAddress}");
            return Unauthorized();
        }

        /// <summary>
        /// Stores the password in the current hash format (old hashes are only upgraded on a successful login,
        /// the plain password is not known otherwise), the "must change" flag and a missing security stamp.
        /// </summary>
        /// <returns>the security stamp of the user</returns>
        private async Task<string> UpdateUser(Guid userId, string rehashPassword, bool mustChangePassword)
        {
            try
            {
                var stored = await DbContext.Users.SingleOrDefaultAsync(a => a.ObjId == userId);
                if (stored == null)
                {
                    return null;
                }

                if (rehashPassword != null)
                {
                    var salt = UserHelper.GenerateNewSalt();
                    stored.Salt = salt;
                    stored.Password = UserHelper.CreatePasswordHash(rehashPassword, salt);
                }

                stored.MustChangePassword = mustChangePassword;
                if (string.IsNullOrEmpty(stored.SecurityStamp))
                {
                    stored.SecurityStamp = UserHelper.NewSecurityStamp();
                }

                await DbContext.SaveChangesAsync();
                DbContext.Entry(stored).State = EntityState.Detached;
                HttpContext?.RequestServices?.GetService<UserClaimsProvider>()?.Invalidate();
                return stored.SecurityStamp;
            }
            catch (Exception e)
            {
                // the login works with the old hash, try again next time
                _logger.LogError(e, "Could not update the user after the login");
                return null;
            }
        }

        [HttpPost]
        [Route("logout")]
        public Task<bool> LogOut()
        {
            // JWT is stateless and only the JwtBearer scheme is registered (it cannot sign out),
            // the client simply drops its token. Removed users and roles are checked on every request (UserClaimsProvider).
            return Task.FromResult(true);
        }
    }
}
