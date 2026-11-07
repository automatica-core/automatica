using System;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Controllers
{
    public class ChangePasswordData
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    /// <summary>
    /// The signed in user's own account
    /// </summary>
    [Route("webapi/account")]
    public class AccountController : BaseController
    {
        /// <summary>
        /// Authenticated, also while the password must be changed (every other policy rejects such a token)
        /// </summary>
        public const string PasswordChangePolicy = "passwordChange";

        private readonly ILogger<AccountController> _logger;
        private readonly IUserCache _userCache;

        public AccountController(AutomaticaContext dbContext, ILogger<AccountController> logger, IUserCache userCache) : base(dbContext)
        {
            _logger = logger;
            _userCache = userCache;
        }

        /// <summary>
        /// Changes the password of the signed in user. Revokes all other tokens of the user (new security stamp).
        /// </summary>
        /// <returns>the user with a new token</returns>
        [HttpPost]
        [Route("password")]
        [Authorize(Policy = PasswordChangePolicy)]
        [AllowPendingPasswordChange]
        [EnableRateLimiting(AuthenticationController.LoginRateLimitPolicy)]
        public async Task<ActionResult<User>> ChangePassword([FromBody] ChangePasswordData data)
        {
            if (data == null || data.OldPassword == null || data.NewPassword == null)
            {
                return BadRequest();
            }

            var idClaim = HttpContext?.User?.FindFirst(UserClaimsProvider.UserIdClaimType)?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await DbContext.Users.SingleOrDefaultAsync(a => a.ObjId == userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var throttle = HttpContext.RequestServices.GetService<LoginThrottle>();
            if (throttle != null && throttle.IsLocked(user.UserName, out _))
            {
                return StatusCode(429);
            }

            if (!UserHelper.VerifyPassword(data.OldPassword, user.Salt, user.Password, out _))
            {
                throttle?.Failed(user.UserName);
                _logger.LogWarning($"Password change of {LoginThrottle.SanitizeForLog(user.UserName)} failed: wrong password");
                return BadRequest(new { error = "WRONG_PASSWORD" });
            }

            var policyError = PasswordPolicy.Validate(data.NewPassword, user.UserName);
            if (policyError != null)
            {
                return BadRequest(new { error = policyError });
            }

            if (data.NewPassword == data.OldPassword)
            {
                return BadRequest(new { error = "PASSWORD_UNCHANGED" });
            }

            var salt = UserHelper.GenerateNewSalt();
            user.Salt = salt;
            user.Password = UserHelper.CreatePasswordHash(data.NewPassword, salt);
            user.MustChangePassword = false;
            user.SecurityStamp = UserHelper.NewSecurityStamp();
            user.ModifiedAt = DateTimeOffset.Now;
            await DbContext.SaveChangesAsync();

            throttle?.Succeeded(user.UserName);
            HttpContext.RequestServices.GetService<UserClaimsProvider>()?.Invalidate();
            _userCache.Remove(user.ObjId);
            _logger.LogInformation($"User {LoginThrottle.SanitizeForLog(user.UserName)} changed the password");

            DbContext.Entry(user).State = EntityState.Detached;
            var loggedIn = await DbContext.Users.AsNoTracking().Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .SingleAsync(a => a.ObjId == userId);
            return TokenIssuer.Login(DbContext, loggedIn);
        }
    }
}
