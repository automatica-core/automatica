using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// The current roles and user groups of a user, as claims.
    /// Used for the login and on every request with a token: the roles and groups in the token are replaced by the
    /// current ones, so a deleted user or a removed role takes effect within <see cref="CacheDuration"/> although the
    /// token itself is valid for days.
    /// </summary>
    public class UserClaimsProvider
    {
        /// <summary>
        /// Claim with the id of the user (tokens issued before it existed only carry the user name).
        /// </summary>
        public const string UserIdClaimType = ClaimTypes.NameIdentifier;

        /// <summary>
        /// Claim with the security stamp of the user at login time: a password change changes the stamp and revokes the token
        /// </summary>
        public const string SecurityStampClaimType = "automatica:stamp";

        /// <summary>
        /// Present (value "true") while the user must change the password: the identity has no roles then
        /// </summary>
        public const string MustChangePasswordClaimType = "automatica:must_change_password";

        public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, (DateTime validUntil, UserClaims claims)> _cache = new();

        public UserClaimsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public record UserClaims(Guid UserId, IReadOnlyList<string> Roles, IReadOnlyList<Guid> UserGroups, IReadOnlyList<User2Role> GroupRoles,
            string SecurityStamp = null, bool MustChangePassword = false);

        /// <summary>
        /// Forget the cached claims (call after users, groups or roles changed).
        /// </summary>
        public void Invalidate()
        {
            _cache.Clear();
        }

        /// <param name="userId">the user id from the token, null for tokens that only have the user name</param>
        /// <param name="userName">the user name from the token</param>
        /// <returns>null if the user does not exist (any more) or the name is ambiguous</returns>
        public UserClaims Get(Guid? userId, string userName)
        {
            var key = userId?.ToString() ?? $"name:{userName}";
            var now = DateTime.UtcNow;

            if (_cache.TryGetValue(key, out var cached) && cached.validUntil > now)
            {
                return cached.claims;
            }

            UserClaims claims;
            using (var db = new AutomaticaContext(_configuration))
            {
                var users = userId.HasValue
                    ? db.Users.AsNoTracking().Where(a => a.ObjId == userId.Value).Take(2).ToList()
                    : db.Users.AsNoTracking().Where(a => a.UserName == userName).Take(2).ToList();

                // a renamed user keeps his old token only if it carries the user id
                claims = users.Count == 1 && users[0].UserName == userName ? Load(db, users[0]) : null;
            }

            _cache[key] = (now.Add(CacheDuration), claims);
            return claims;
        }

        /// <summary>
        /// Roles (direct and through the user groups) and user groups of the user, with one query per table.
        /// </summary>
        public static UserClaims Load(AutomaticaContext db, User user)
        {
            var directRoles = db.User2Roles.AsNoTracking()
                .Where(a => a.This2User == user.ObjId)
                .Include(a => a.This2RoleNavigation)
                .ToList();

            var groupIds = db.User2Groups.AsNoTracking()
                .Where(a => a.This2User == user.ObjId)
                .Select(a => a.This2UserGroup)
                .ToList();

            var groupRoles = db.UserGroup2Roles.AsNoTracking()
                .Where(a => groupIds.Contains(a.This2UserGroup))
                .Include(a => a.This2RoleNavigation)
                .ToList();

            var roles = directRoles.Select(a => a.This2RoleNavigation?.Key)
                .Concat(groupRoles.Select(a => a.This2RoleNavigation?.Key))
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .ToList();

            var user2Roles = groupRoles.Select(a => new User2Role
            {
                This2User = user.ObjId,
                This2Role = a.This2Role,
                This2RoleNavigation = a.This2RoleNavigation
            }).ToList();

            return new UserClaims(user.ObjId, roles, groupIds.Distinct().ToList(), user2Roles, user.SecurityStamp, user.MustChangePassword);
        }

        /// <summary>
        /// Checks the user of a token (still existing, security stamp) and replaces its roles and user groups with the current ones.
        /// </summary>
        /// <returns>null if the identity is valid, otherwise the reason it is not</returns>
        public string Refresh(ClaimsIdentity identity)
        {
            var idClaim = identity.FindFirst(UserIdClaimType)?.Value ?? identity.FindFirst("nameid")?.Value;
            Guid? userId = Guid.TryParse(idClaim, out var id) ? id : null;
            var userName = identity.Name ?? identity.FindFirst("unique_name")?.Value;

            var claims = Get(userId, userName);
            if (claims == null)
            {
                return "The user of the token does not exist anymore";
            }

            // a password change (or a reset by an admin) revokes all older tokens of the user
            if (!IsStampValid(identity, claims))
            {
                return "The token was revoked";
            }

            Apply(identity, claims);
            return null;
        }

        /// <summary>
        /// Replaces the role and user group claims of the identity with the given ones.
        /// </summary>
        public static void Apply(ClaimsIdentity identity, UserClaims claims)
        {
            foreach (var claim in identity.Claims
                         .Where(a => a.Type == identity.RoleClaimType || a.Type == ClaimTypes.Role || a.Type == "role" || a.Type == UserGroup.ClaimType ||
                                     a.Type == MustChangePasswordClaimType)
                         .ToList())
            {
                identity.TryRemoveClaim(claim);
            }

            if (claims.MustChangePassword)
            {
                // nothing but the password change (webapi/account/password) until a new password is set
                identity.AddClaim(new Claim(MustChangePasswordClaimType, "true"));
                return;
            }

            foreach (var role in claims.Roles)
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }

            foreach (var group in claims.UserGroups)
            {
                identity.AddClaim(new Claim(UserGroup.ClaimType, group.ToString()));
            }
        }

        /// <returns>true if the stamp in the token matches the current stamp of the user (tokens without a stamp are rejected)</returns>
        public static bool IsStampValid(ClaimsIdentity identity, UserClaims claims)
        {
            var tokenStamp = identity.FindFirst(SecurityStampClaimType)?.Value;
            if (string.IsNullOrEmpty(tokenStamp) || string.IsNullOrEmpty(claims.SecurityStamp))
            {
                return false;
            }

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(tokenStamp), System.Text.Encoding.UTF8.GetBytes(claims.SecurityStamp));
        }
    }
}
