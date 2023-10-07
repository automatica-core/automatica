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
using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Controllers
{
    public class UserAuthData
    {
        public UserAuthData()
        {
            
        }

        [Key("username")]
        public string Username { get; set; }

        [Key("password")]
        public string Password { get; set; }
    }

    [Route("webapi/auth")]
    [AllowAnonymous]
    public class AuthenticationController : BaseController
    {

        public AuthenticationController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpPost]
        [Route("login")]
        public async Task<User> Login([FromBody]UserAuthData data)
        {
            var user = await DbContext.Users.Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation).SingleOrDefaultAsync(a => a.UserName == data.Username);

            if (user == null)
            {
                return null;
            }

            var salt = user.Salt;

            var hash = UserHelper.HashPassword(data.Password, salt);

            if (hash == user.Password)
            {
                user = LoginUser(user);
                user.Salt = null;
                user.Password = "";
                return user;
            }

            return null;
        }
        [HttpPost]
        [Route("logout")]
        public async Task<bool> LogOut()
        {
            await HttpContext.SignOutAsync();
            return true;
        }

        private User LoginUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            foreach (var role in user.InverseThis2Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.This2RoleNavigation.Key));
            }

            var userGroups = DbContext.User2Groups.Where(a => a.This2User == user.ObjId).ToList();

            foreach (var userGroup in userGroups)
            {
                var roles = DbContext.UserGroup2Roles.Where(a => a.This2UserGroup == userGroup.This2UserGroup)
                    .Include(a => a.This2RoleNavigation).ToList();

                foreach (var userGroupRoles in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userGroupRoles.This2RoleNavigation.Key));

                    user.InverseThis2Roles.Add(new User2Role()
                    {
                        This2User = user.ObjId,
                        This2Role = userGroupRoles.This2Role,
                        This2RoleNavigation = userGroupRoles.This2RoleNavigation
                    });
                }

                claims.Add(new Claim(UserGroup.ClaimType, userGroup.This2UserGroup.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims);

            var tokenHandler = new JwtSecurityTokenHandler();
            var serverUid = ServerInfo.ServerUid.ToByteArray();
            var key = new byte[32];
            Array.Copy(serverUid, 0, key, 0, 16);
            Array.Copy(serverUid, 0, key, 16, 16);
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
