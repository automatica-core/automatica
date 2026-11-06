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

        public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, (DateTime validUntil, UserClaims claims)> _cache = new();

        public UserClaimsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public record UserClaims(Guid UserId, IReadOnlyList<string> Roles, IReadOnlyList<Guid> UserGroups, IReadOnlyList<User2Role> GroupRoles);

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

            return new UserClaims(user.ObjId, roles, groupIds.Distinct().ToList(), user2Roles);
        }

        /// <summary>
        /// Replaces the role and user group claims of the identity with the given ones.
        /// </summary>
        public static void Apply(ClaimsIdentity identity, UserClaims claims)
        {
            foreach (var claim in identity.Claims
                         .Where(a => a.Type == identity.RoleClaimType || a.Type == ClaimTypes.Role || a.Type == "role" || a.Type == UserGroup.ClaimType)
                         .ToList())
            {
                identity.TryRemoveClaim(claim);
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
    }
}
