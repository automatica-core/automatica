using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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

        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(AutomaticaContext dbContext, ILogger<AuthenticationController> logger) : base(dbContext)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("login")]
        [EnableRateLimiting(LoginRateLimitPolicy)]
        public async Task<ActionResult<User>> Login([FromBody]UserAuthData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Username) || data.Password == null)
            {
                return Unauthorized();
            }

            // user names should be unique, but older databases may contain duplicates: the password decides
            var candidates = await DbContext.Users.AsNoTracking().Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .Where(a => a.UserName == data.Username).ToListAsync();

            foreach (var user in candidates)
            {
                if (!UserHelper.VerifyPassword(data.Password, user.Salt, user.Password, out var needsRehash))
                {
                    continue;
                }

                if (needsRehash)
                {
                    await UpgradePasswordHash(user.ObjId, data.Password);
                }

                var loggedIn = LoginUser(user);
                loggedIn.Salt = null;
                loggedIn.Password = "";
                return loggedIn;
            }

            _logger.LogWarning($"Failed login for user {data.Username} from {HttpContext?.Connection?.RemoteIpAddress}");
            return Unauthorized();
        }

        /// <summary>
        /// Stores the password in the current hash format (old hashes are only upgraded on a successful login,
        /// the plain password is not known otherwise).
        /// </summary>
        private async Task UpgradePasswordHash(Guid userId, string password)
        {
            try
            {
                var stored = await DbContext.Users.SingleOrDefaultAsync(a => a.ObjId == userId);
                if (stored == null)
                {
                    return;
                }

                var salt = UserHelper.GenerateNewSalt();
                stored.Salt = salt;
                stored.Password = UserHelper.CreatePasswordHash(password, salt);
                await DbContext.SaveChangesAsync();
                DbContext.Entry(stored).State = EntityState.Detached;
            }
            catch (Exception e)
            {
                // the login works with the old hash, try again next time
                _logger.LogError(e, "Could not upgrade the password hash");
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

        private User LoginUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(UserClaimsProvider.UserIdClaimType, user.ObjId.ToString()),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? "")
            };

            // direct roles and the roles of the user groups, one query per table
            var userClaims = UserClaimsProvider.Load(DbContext, user);

            foreach (var role in userClaims.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // the client reads the roles of the user groups from the user's roles
            user.InverseThis2Roles.AddRange(userClaims.GroupRoles);

            foreach (var userGroup in userClaims.UserGroups)
            {
                claims.Add(new Claim(UserGroup.ClaimType, userGroup.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = JwtSigningKey.Get();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            return user;
        }

    }
}
