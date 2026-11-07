using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.Model.Models.User;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Tests.Http
{
    public class UserManagementHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/usermgm";
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;
        private readonly Guid _adminId;

        public UserManagementHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken(out _adminId));
        }

        private User NewUser(string password = WebApiHost.StrongPassword, params string[] roles) => new()
        {
            ObjId = Guid.NewGuid(),
            UserName = _host.UniqueName("managed"),
            FirstName = "First",
            LastName = "Last",
            Description = "",
            Password = password,
            PasswordConfirm = password,
            InverseThis2Roles = roles.Select(a => new User2Role { This2Role = _host.RoleId(a) }).ToList(),
            InverseThis2UserGroups = new List<User2Group>()
        };

        private User Stored(Guid id)
        {
            using var db = _host.CreateDbContext();
            return db.Users.AsNoTracking().Include(a => a.InverseThis2Roles).Include(a => a.InverseThis2UserGroups).SingleOrDefault(a => a.ObjId == id);
        }

        private async Task<string> Error(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        [Fact]
        public async Task User_CreateGetUpdateDelete_RoundTrip()
        {
            var user = NewUser(WebApiHost.StrongPassword, Role.VisuRole);

            var created = await _admin.PostJson($"{Url}/user", user);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            Assert.Equal(user.UserName, (string)(await created.ReadJson())["userName"]);

            var got = await (await _admin.GetAsync($"{Url}/user/{user.ObjId}")).ReadJson();
            Assert.Equal(user.UserName, (string)got["userName"]);
            Assert.Contains((JArray)await (await _admin.GetAsync($"{Url}/users")).ReadJson(), a => (Guid)a["objId"] == user.ObjId);

            Assert.Equal(HttpStatusCode.OK, (await _host.LoginAsync(user.UserName, WebApiHost.StrongPassword)).StatusCode);

            user.FirstName = "Changed";
            user.Password = null;
            user.PasswordConfirm = null;
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/user", user)).StatusCode);
            Assert.Equal("Changed", Stored(user.ObjId).FirstName);
            // no new password posted: the old one keeps working
            Assert.Equal(HttpStatusCode.OK, (await _host.LoginAsync(user.UserName, WebApiHost.StrongPassword)).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/user/{user.ObjId}")).StatusCode);
            Assert.Null(Stored(user.ObjId));
            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.LoginAsync(user.UserName, WebApiHost.StrongPassword)).StatusCode);
        }

        [Fact]
        public async Task Users_Responses_NeverContainPasswordHashesOrSalts()
        {
            var user = NewUser();
            var created = await _admin.PostJson($"{Url}/user", user);
            var stored = Stored(user.ObjId);

            foreach (var body in new[]
                     {
                         (await created.ReadJson()).ToString(),
                         await (await _admin.GetAsync($"{Url}/users")).Content.ReadAsStringAsync(),
                         await (await _admin.GetAsync($"{Url}/user/{user.ObjId}")).Content.ReadAsStringAsync()
                     })
            {
                Assert.DoesNotContain(stored.Password, body);
                Assert.DoesNotContain(stored.Salt, body);
                Assert.DoesNotContain(stored.SecurityStamp, body);
                Assert.DoesNotContain(WebApiHost.StrongPassword, body);
            }
        }

        [Fact]
        public async Task SaveUser_StoresAHashNotThePassword()
        {
            var user = NewUser();
            await _admin.PostJson($"{Url}/user", user);

            var stored = Stored(user.ObjId);
            Assert.NotEqual(WebApiHost.StrongPassword, stored.Password);
            Assert.False(string.IsNullOrEmpty(stored.Salt));
            Assert.False(string.IsNullOrEmpty(stored.SecurityStamp));
        }

        [Fact]
        public async Task SaveUser_PasswordMismatch_Returns400()
        {
            var user = NewUser();
            user.PasswordConfirm = "Other-Horse-9";
            Assert.Contains("do not match", await Error(await _admin.PostJson($"{Url}/user", user)));
            Assert.Null(Stored(user.ObjId));
        }

        [Theory]
        [InlineData("short", "PASSWORD_TOO_SHORT")]
        [InlineData("password", "PASSWORD_TOO_WEAK")]
        [InlineData("aaaaaaaaaaaa", "PASSWORD_TOO_WEAK")]
        public async Task SaveUser_PasswordPolicy_Returns400WithErrorKey(string password, string error)
        {
            var user = NewUser(password);
            Assert.Contains(error, await Error(await _admin.PostJson($"{Url}/user", user)));
            Assert.Null(Stored(user.ObjId));
        }

        [Fact]
        public async Task SaveUser_PasswordEqualsUserName_Returns400()
        {
            var user = NewUser();
            user.UserName = _host.UniqueName("samename");
            user.Password = user.PasswordConfirm = user.UserName;
            Assert.Contains("PASSWORD_EQUALS_USERNAME", await Error(await _admin.PostJson($"{Url}/user", user)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveUser_EmptyUserName_Returns400(string userName)
        {
            var user = NewUser();
            user.UserName = userName;
            Assert.Contains("must not be empty", await Error(await _admin.PostJson($"{Url}/user", user)));
        }

        [Fact]
        public async Task SaveUser_DuplicateUserName_Returns400()
        {
            var first = NewUser();
            await _admin.PostJson($"{Url}/user", first);
            var second = NewUser();
            second.UserName = first.UserName;

            Assert.Contains("already exists", await Error(await _admin.PostJson($"{Url}/user", second)));
            Assert.Null(Stored(second.ObjId));
        }

        [Fact]
        public async Task SaveUser_NullBody_Returns400()
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"{Url}/user", HttpTestExtensions.RawJson("null"))).StatusCode);
        }

        [Fact]
        public async Task SaveUser_NewPassword_RevokesTheTokensOfTheUser()
        {
            var user = NewUser(WebApiHost.StrongPassword, Role.VisuRole);
            await _admin.PostJson($"{Url}/user", user);
            var token = _host.TokenFor(user.ObjId);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(token).GetAsync("webapi/Ping")).StatusCode);

            user.Password = user.PasswordConfirm = "Reset-By-Admin-3";
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/user", user)).StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient(token).GetAsync("webapi/Ping")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _host.LoginAsync(user.UserName, "Reset-By-Admin-3")).StatusCode);
        }

        [Fact]
        public async Task SaveUser_MustChangePassword_ForcesThePasswordChangeAtLogin()
        {
            var user = NewUser(WebApiHost.StrongPassword, Role.VisuRole);
            user.MustChangePassword = true;
            await _admin.PostJson($"{Url}/user", user);

            var body = await (await _host.LoginAsync(user.UserName, WebApiHost.StrongPassword)).ReadJson();
            Assert.True((bool)body["mustChangePassword"]);
        }

        [Fact]
        public async Task SaveUser_RoleRemoved_TakesEffectOnTheExistingToken()
        {
            var user = NewUser(WebApiHost.StrongPassword, Role.AdminRole);
            await _admin.PostJson($"{Url}/user", user);
            var token = _host.TokenFor(user.ObjId);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(token).GetAsync($"{Url}/roles")).StatusCode);

            user.Password = user.PasswordConfirm = null;
            user.InverseThis2Roles = new List<User2Role> { new() { This2Role = _host.RoleId(Role.VisuRole) } };
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/user", user)).StatusCode);

            // the user management invalidates the claims cache: no 30 s window
            Assert.Equal(HttpStatusCode.Forbidden, (await _host.CreateClient(token).GetAsync($"{Url}/roles")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient(token).GetAsync("webapi/Ping")).StatusCode);
        }

        [Fact]
        public async Task SaveUser_CreatedAtIsServerSide()
        {
            var user = NewUser();
            user.CreatedAt = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
            await _admin.PostJson($"{Url}/user", user);
            var createdAt = Stored(user.ObjId).CreatedAt;

            user.Password = user.PasswordConfirm = null;
            user.CreatedAt = new DateTimeOffset(2002, 2, 2, 0, 0, 0, TimeSpan.Zero);
            await _admin.PostJson($"{Url}/user", user);

            Assert.NotEqual(2001, createdAt.Year);
            Assert.Equal(createdAt, Stored(user.ObjId).CreatedAt);
        }

        [Fact]
        public async Task DeleteUser_CurrentUser_Returns400()
        {
            Assert.Contains("current user", await Error(await _admin.DeleteAsync($"{Url}/user/{_adminId}")));
            Assert.NotNull(Stored(_adminId));
        }

        [Fact]
        public async Task DeleteUser_Unknown_IsOk()
        {
            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/user/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task SaveUsers_EmptyList_DeletesNothing()
        {
            int before;
            await using (var db = _host.CreateDbContext())
            {
                before = await db.Users.CountAsync();
            }

            var response = await _admin.PostJson($"{Url}/users", Array.Empty<User>());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using (var db = _host.CreateDbContext())
            {
                Assert.Equal(before, await db.Users.CountAsync());
            }
        }

        [Fact]
        public async Task SaveUsers_NullBody_Returns400()
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"{Url}/users", HttpTestExtensions.RawJson("null"))).StatusCode);
        }

        [Fact]
        public async Task SaveUsers_ListWithoutTheCurrentUser_Returns400()
        {
            // posting only another user would delete everybody else, the current admin included
            var response = await _admin.PostJson($"{Url}/users", new[] { NewUser() });
            Assert.Contains("current user", await Error(response));
            Assert.NotNull(Stored(_adminId));
        }

        [Fact]
        public async Task SaveUsers_DuplicateNamesInTheList_Returns400()
        {
            var a = NewUser();
            var b = NewUser();
            b.UserName = a.UserName;
            var users = (await (await _admin.GetAsync($"{Url}/users")).Read<List<User>>()).Concat(new[] { a, b }).ToList();

            Assert.Contains("already exists", await Error(await _admin.PostJson($"{Url}/users", users)));
        }

        [Fact]
        public async Task UserGroup_CreateGetUpdateDelete_RoundTrip()
        {
            var group = new UserGroup
            {
                ObjId = Guid.NewGuid(),
                Name = _host.UniqueName("group"),
                Description = "",
                InverseThis2Roles = new List<UserGroup2Role> { new() { This2Role = _host.RoleId(Role.VisuRole) } }
            };

            var created = await _admin.PostJson($"{Url}/usergroup", group);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var got = await (await _admin.GetAsync($"{Url}/usergroup/{group.ObjId}")).ReadJson();
            Assert.Equal(group.Name, (string)got["name"]);
            await using (var db = _host.CreateDbContext())
            {
                Assert.Equal(_host.RoleId(Role.VisuRole), (await db.UserGroup2Roles.SingleAsync(a => a.This2UserGroup == group.ObjId)).This2Role);
            }

            group.Name += "-renamed";
            group.InverseThis2Roles = new List<UserGroup2Role>();
            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/usergroup", group)).StatusCode);
            await using (var db = _host.CreateDbContext())
            {
                Assert.Equal(group.Name, (await db.UserGroups.AsNoTracking().SingleAsync(a => a.ObjId == group.ObjId)).Name);
                Assert.False(await db.UserGroup2Roles.AnyAsync(a => a.This2UserGroup == group.ObjId));
            }

            Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync($"{Url}/usergroup/{group.ObjId}")).StatusCode);
            await using (var db = _host.CreateDbContext())
            {
                Assert.False(await db.UserGroups.AnyAsync(a => a.ObjId == group.ObjId));
            }
        }

        [Fact]
        public async Task UserGroup_PostedUsers_AreNeverUpdated()
        {
            var userId = _host.CreateUser(_host.UniqueName("member"), WebApiHost.StrongPassword, new[] { Role.VisuRole });
            var stored = Stored(userId);
            var group = new UserGroup
            {
                ObjId = Guid.NewGuid(),
                Name = _host.UniqueName("group"),
                Description = "",
                InverseThis2Users = new List<User2Group>
                {
                    new()
                    {
                        This2User = userId,
                        This2UserNavigation = new User { ObjId = userId, UserName = stored.UserName, Password = "overwritten", Salt = "x" }
                    }
                }
            };

            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/usergroup", group)).StatusCode);

            var after = Stored(userId);
            Assert.Equal(stored.Password, after.Password);
            Assert.Equal(stored.Salt, after.Salt);
            Assert.Equal(HttpStatusCode.OK, (await _host.LoginAsync(stored.UserName, WebApiHost.StrongPassword)).StatusCode);
        }

        [Fact]
        public async Task UserGroup_NullBody_Returns400()
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"{Url}/usergroup", HttpTestExtensions.RawJson("null"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"{Url}/usergroups", HttpTestExtensions.RawJson("null"))).StatusCode);
        }

        [Fact]
        public async Task UserGroups_EmptyList_DeletesNothing()
        {
            int before;
            await using (var db = _host.CreateDbContext())
            {
                before = await db.UserGroups.CountAsync();
            }

            Assert.Equal(HttpStatusCode.OK, (await _admin.PostJson($"{Url}/usergroups", Array.Empty<UserGroup>())).StatusCode);

            await using (var db = _host.CreateDbContext())
            {
                Assert.Equal(before, await db.UserGroups.CountAsync());
            }
        }

        [Fact]
        public async Task Roles_ContainAdminAndVisu()
        {
            var roles = (JArray)await (await _admin.GetAsync($"{Url}/roles")).ReadJson();
            Assert.Contains(roles, a => (string)a["key"] == Role.AdminRole);
            Assert.Contains(roles, a => (string)a["key"] == Role.VisuRole);
        }
    }

    /// <summary>
    /// The last administrator (direct role or through a group) can neither be deleted nor lose the role.
    /// Own host per test: the checks depend on how many administrators exist.
    /// </summary>
    public class UserManagementLastAdminTests : IDisposable
    {
        private const string Url = "webapi/usermgm";
        private readonly WebApiHost _host = new();

        public void Dispose() => _host.Dispose();

        /// <summary>
        /// Removes the administrator role of the seeded sa user, the returned user is the only administrator
        /// </summary>
        private (Guid id, HttpClient client) OnlyAdmin(bool throughGroup, out Guid adminGroup)
        {
            using (var db = _host.CreateDbContext())
            {
                var adminRole = db.Roles.Single(a => a.Key == Role.AdminRole).ObjId;
                db.User2Roles.RemoveRange(db.User2Roles.Where(a => a.This2Role == adminRole));
                db.UserGroup2Roles.RemoveRange(db.UserGroup2Roles.Where(a => a.This2Role == adminRole));
                db.SaveChanges();
            }

            adminGroup = Guid.Empty;
            Guid id;
            if (throughGroup)
            {
                adminGroup = _host.CreateUserGroup("only-admins", Role.AdminRole);
                id = _host.CreateUser("onlyadmin", WebApiHost.StrongPassword, Array.Empty<string>(), new[] { adminGroup });
            }
            else
            {
                id = _host.CreateUser("onlyadmin", WebApiHost.StrongPassword, new[] { Role.AdminRole });
            }

            return (id, _host.CreateClient(_host.TokenFor(id)));
        }

        [Fact]
        public async Task SaveUser_LastAdminLosesTheRole_Returns400()
        {
            var (id, client) = OnlyAdmin(false, out _);
            var self = new User
            {
                ObjId = id,
                UserName = "onlyadmin",
                InverseThis2Roles = new List<User2Role> { new() { This2Role = _host.RoleId(Role.VisuRole) } },
                InverseThis2UserGroups = new List<User2Group>()
            };

            var response = await client.PostJson($"{Url}/user", self);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("last administrator", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/roles")).StatusCode);
        }

        [Fact]
        public async Task DeleteUserGroup_LastAdminGroup_Returns400()
        {
            var (_, client) = OnlyAdmin(true, out var adminGroup);

            var response = await client.DeleteAsync($"{Url}/usergroup/{adminGroup}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.True(await db.UserGroups.AnyAsync(a => a.ObjId == adminGroup));
        }

        [Fact]
        public async Task SaveUserGroup_LastAdminGroupLosesTheRole_Returns400()
        {
            var (_, client) = OnlyAdmin(true, out var adminGroup);
            var group = new UserGroup { ObjId = adminGroup, Name = "only-admins", Description = "", InverseThis2Roles = new List<UserGroup2Role>() };

            var response = await client.PostJson($"{Url}/usergroup", group);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/roles")).StatusCode);
        }

        [Fact]
        public async Task SaveUserGroup_WithoutRolesPosted_KeepsTheRoles()
        {
            var (_, client) = OnlyAdmin(true, out var adminGroup);
            var group = new UserGroup { ObjId = adminGroup, Name = "renamed-admins", Description = "", InverseThis2Roles = null };

            Assert.Equal(HttpStatusCode.OK, (await client.PostJson($"{Url}/usergroup", group)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Url}/roles")).StatusCode);
        }

        [Fact]
        public async Task SaveUserGroups_ListWithoutTheAdminGroup_Returns400()
        {
            var (_, client) = OnlyAdmin(true, out var adminGroup);
            var other = new UserGroup { ObjId = Guid.NewGuid(), Name = "other", Description = "" };

            var response = await client.PostJson($"{Url}/usergroups", new[] { other });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.True(await db.UserGroups.AnyAsync(a => a.ObjId == adminGroup));
        }

        [Fact]
        public async Task DeleteUser_AnotherAdminExists_IsAllowed()
        {
            var (_, client) = OnlyAdmin(false, out _);
            var second = _host.CreateUser("secondadmin", WebApiHost.StrongPassword, new[] { Role.AdminRole });

            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{Url}/user/{second}")).StatusCode);
            await using var db = _host.CreateDbContext();
            Assert.False(await db.Users.AnyAsync(a => a.ObjId == second));
        }
    }
}
