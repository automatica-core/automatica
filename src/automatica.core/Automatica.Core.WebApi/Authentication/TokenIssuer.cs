using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.IdentityModel.Tokens;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// Issues the login token of a user
    /// </summary>
    public static class TokenIssuer
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

        /// <summary>
        /// Sets <see cref="User.Token"/> and the group roles of <paramref name="user"/> (the client reads them from the user)
        /// and clears the password fields.
        /// </summary>
        public static User Login(AutomaticaContext db, User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(UserClaimsProvider.UserIdClaimType, user.ObjId.ToString()),
                new Claim(UserClaimsProvider.SecurityStampClaimType, user.SecurityStamp ?? string.Empty),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? "")
            };

            // direct roles and the roles of the user groups, one query per table
            var userClaims = UserClaimsProvider.Load(db, user);

            if (user.MustChangePassword)
            {
                // the client shows the password change, every other request is rejected until then
                claims.Add(new Claim(UserClaimsProvider.MustChangePasswordClaimType, "true"));
                user.InverseThis2Roles.Clear();
            }
            else
            {
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
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = JwtSigningKey.Get();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(TokenLifetime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            user.Token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            user.Salt = null;
            user.Password = "";
            user.PasswordConfirm = null;
            user.SecurityStamp = null;

            return user;
        }
    }
}
