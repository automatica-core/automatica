using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/usermgm")]
    [Authorize(Policy = Role.AdminRole)]
    public class UserManagementController : BaseController
    {
        private readonly ILogger<UserManagementController> _logger;
        private readonly IUserCache _userCache;
        private readonly IUserGroupsCache _userGroupsCache;
        private readonly IConfiguration _config;

        public UserManagementController(ILogger<UserManagementController> logger, AutomaticaContext DbContext, IUserCache userCache, IUserGroupsCache userGroupsCache, IConfiguration config) : base(DbContext)
        {
            _logger = logger;
            _userCache = userCache;
            _userGroupsCache = userGroupsCache;
            _config = config;
        }


        [HttpGet]
        [Route("usergroups")]
        public ICollection<UserGroup> GetUserGroups()
        {
            return _userGroupsCache.All();
        }

        /// <summary>
        /// Roles and groups are re-read on every request with a token (cached for a few seconds), apply a change at once.
        /// </summary>
        private void InvalidateUserClaims()
        {
            HttpContext?.RequestServices?.GetService<UserClaimsProvider>()?.Invalidate();
        }

        [HttpDelete]
        [Route("usergroup/{id}")]
        public async Task<IActionResult> DeleteUserGroup(Guid id)
        {
            if (WouldRemoveLastAdmin(Array.Empty<Guid>(), new[] { id }))
            {
                return BadRequest("Cannot delete the last user group with administrator rights");
            }

            var deleted = false;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();

                try
                {
                    var userGroup = DbContext.UserGroups.SingleOrDefault(a => a.ObjId == id);
                    if (userGroup != null)
                    {
                        DbContext.UserGroups.Remove(userGroup);
                        await DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        deleted = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not delete usergroup");
                    throw;
                }
            });

            if (deleted)
            {
                _userGroupsCache.Remove(id);
                InvalidateUserClaims();
            }
            return Ok();
        }

        [HttpPost]
        [Route("usergroup")]
        public async Task<ActionResult<UserGroup>> SaveUserGroup([FromBody] UserGroup instance)
        {
            if (instance == null)
            {
                return BadRequest();
            }

            if (WouldRemoveLastAdmin(Array.Empty<Guid>(), GroupsLosingAdminRole(new[] { instance })))
            {
                return BadRequest("Cannot remove the administrator role from the last user group with administrator rights");
            }

            var roles = instance.InverseThis2Roles;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            var attempt = 0;
            await strategy.ExecuteAsync(async
                () =>
            {
                if (attempt++ > 0)
                {
                    // retry: the same input as the first attempt, without the entities it tracked
                    DbContext.ChangeTracker.Clear();
                    instance.InverseThis2Roles = roles;
                }
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    SaveUserGroupInternal(instance);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }
            });

            // the posted object has its roles detached, reload the group from the database
            _userGroupsCache.Clear();
            InvalidateUserClaims();
            return GetUserGroup(instance.ObjId);
        }

        /// <returns>the posted groups that have the administrator role now, but not after saving them</returns>
        private ICollection<Guid> GroupsLosingAdminRole(IEnumerable<UserGroup> groups)
        {
            var adminRoleIds = DbContext.Roles.AsNoTracking().Where(a => a.Key == Role.AdminRole).Select(a => a.ObjId).ToList();
            var ret = new List<Guid>();

            foreach (var group in groups)
            {
                // without posted roles, the roles of the group are not changed
                if (group.InverseThis2Roles == null)
                {
                    continue;
                }

                var isAdminGroup = DbContext.UserGroup2Roles.AsNoTracking()
                    .Any(a => a.This2UserGroup == group.ObjId && adminRoleIds.Contains(a.This2Role));
                var staysAdminGroup = group.InverseThis2Roles.Any(a => adminRoleIds.Contains(a.This2Role));

                if (isAdminGroup && !staysAdminGroup)
                {
                    ret.Add(group.ObjId);
                }
            }

            return ret;
        }

        private void SaveUserGroupInternal(UserGroup instance)
        {
            var existing = DbContext.UserGroups.AsNoTracking().SingleOrDefault(a => a.ObjId == instance.ObjId);
            var roles = instance.InverseThis2Roles;
            instance.InverseThis2Roles = null;
            // never update the posted users of the group (would overwrite e.g. their password hashes)
            instance.InverseThis2Users = null;

            if (existing == null)
            {
                DbContext.UserGroups.Add(instance);
            }
            else
            {
                DbContext.UserGroups.Update(instance);
            }

            if (roles == null)
            {
                return;
            }

            foreach (var role in roles)
            {
                // only the relation row, never the posted role/group graph
                role.This2UserGroup = instance.ObjId;
                role.This2RoleNavigation = null;
                role.This2UserGroupNavigation = null;

                var rolesExisting = DbContext.UserGroup2Roles.SingleOrDefault(a =>
                    a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

                if (rolesExisting != null)
                {
                    continue;
                }

                DbContext.UserGroup2Roles.Add(role);
            }

            // roles of this group which are no longer posted
            var roleIds = roles.Select(o => o.This2Role).ToList();
            var removedUserRolesList = DbContext.UserGroup2Roles
                .Where(c => c.This2UserGroup == instance.ObjId && !roleIds.Contains(c.This2Role))
                .ToList();
            DbContext.RemoveRange(removedUserRolesList);
        }


        [HttpPost]
        [Route("usergroups")]
        public async Task<ActionResult<ICollection<UserGroup>>> SaveUserGroups([FromBody]IList<UserGroup> instances)
        {
            if (instances == null)
            {
                return BadRequest();
            }

            // an empty list is treated as "nothing to delete"
            var removedIds = new List<Guid>();
            if (instances.Count > 0)
            {
                var postedIds = instances.Select(o => o.ObjId).ToList();
                removedIds = DbContext.UserGroups.AsNoTracking().Where(c => !postedIds.Contains(c.ObjId)).Select(a => a.ObjId).ToList();
            }

            if (WouldRemoveLastAdmin(Array.Empty<Guid>(), removedIds.Concat(GroupsLosingAdminRole(instances)).ToList()))
            {
                return BadRequest("Cannot delete the last user group with administrator rights");
            }

            var postedRoles = instances.Select(a => a.InverseThis2Roles).ToList();
            var strategy = DbContext.Database.CreateExecutionStrategy();
            var attempt = 0;
            await strategy.ExecuteAsync(async
                () =>
            {
                if (attempt++ > 0)
                {
                    // retry: the same input as the first attempt, without the entities it tracked
                    DbContext.ChangeTracker.Clear();
                    for (var i = 0; i < instances.Count; i++)
                    {
                        instances[i].InverseThis2Roles = postedRoles[i];
                    }
                }
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var instance in instances)
                    {
                        SaveUserGroupInternal(instance);
                    }

                    var removedList = DbContext.UserGroups.Where(c => removedIds.Contains(c.ObjId)).ToList();
                    DbContext.RemoveRange(removedList);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }
            });

            _userGroupsCache.Clear();
            InvalidateUserClaims();
            return Ok(GetUserGroups());
        }

        [HttpGet]
        [Route("users")]
        public ICollection<User> GetUsers()
        {
            return _userCache.All();
        }


        [HttpGet]
        [Route("user/{id}")]
        public User GetUser(Guid id)
        {
            return _userCache.Get(id);
        }

        [HttpGet]
        [Route("usergroup/{id}")]
        public UserGroup GetUserGroup(Guid id)
        {
            return _userGroupsCache.Get(id);
        }

        [HttpGet]
        [Route("roles")]
        public IList<Role> GetRoles()
        {
            return DbContext.Roles.AsNoTracking().ToList();
        }

        [HttpDelete]
        [Route("user/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var guardError = CheckUserRemoval(new[] { id });
            if (guardError != null)
            {
                return BadRequest(guardError);
            }

            var deleted = false;
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();

                try
                {
                    var user = DbContext.Users.SingleOrDefault(a => a.ObjId == id);
                    if (user != null)
                    {
                        DbContext.Users.Remove(user);
                        await DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        deleted = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not delete user");
                    throw;
                }
            });

            if (deleted)
            {
                _userCache.Remove(id);
                InvalidateUserClaims();
            }
            return Ok();
        }

        [HttpPost]
        [Route("user")]
        public async Task<ActionResult<User>> SaveUser([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest();
            }
            if (HasPasswordMismatch(user))
            {
                return BadRequest("Password and password confirmation do not match");
            }
            var passwordError = GetPasswordPolicyError(user);
            if (passwordError != null)
            {
                return BadRequest(passwordError);
            }

            var error = CheckUsers(new[] { user });
            if (error != null)
            {
                return BadRequest(error);
            }

            await SaveUserInternal(user);
            InvalidateUserClaims();
            return GetUser(user.ObjId);
        }

        /// <returns>an error message if the posted users must not be saved (duplicate user name, last administrator loses the role)</returns>
        private string CheckUsers(ICollection<User> users, ICollection<Guid> removedUserIds = null)
        {
            if (users.Any(a => string.IsNullOrWhiteSpace(a.UserName)))
            {
                return "The user name must not be empty";
            }

            if (users.GroupBy(a => a.UserName).Any(a => a.Count() > 1))
            {
                return "The user name already exists";
            }

            var postedIds = users.Select(a => a.ObjId).ToList();
            var postedNames = users.Select(a => a.UserName).ToList();
            var ignoredIds = postedIds.Concat(removedUserIds ?? Array.Empty<Guid>()).ToList();
            if (DbContext.Users.AsNoTracking().Any(a => postedNames.Contains(a.UserName) && !ignoredIds.Contains(a.ObjId)))
            {
                return "The user name already exists";
            }

            if (WouldRemoveLastAdmin(UsersLosingAdminRights(users).Concat(removedUserIds ?? Array.Empty<Guid>()).ToList(), Array.Empty<Guid>()))
            {
                return "Cannot remove the administrator rights of the last administrator";
            }

            return null;
        }

        /// <returns>the posted users that are administrators now (directly or through a group), but not after saving them</returns>
        private ICollection<Guid> UsersLosingAdminRights(IEnumerable<User> users)
        {
            var adminRoleIds = DbContext.Roles.AsNoTracking().Where(a => a.Key == Role.AdminRole).Select(a => a.ObjId).ToList();
            var adminGroupIds = DbContext.UserGroup2Roles.AsNoTracking()
                .Where(a => adminRoleIds.Contains(a.This2Role))
                .Select(a => a.This2UserGroup)
                .ToList();
            var ret = new List<Guid>();

            foreach (var user in users)
            {
                var isAdmin = DbContext.User2Roles.AsNoTracking().Any(a => a.This2User == user.ObjId && adminRoleIds.Contains(a.This2Role)) ||
                              DbContext.User2Groups.AsNoTracking().Any(a => a.This2User == user.ObjId && adminGroupIds.Contains(a.This2UserGroup));

                // the save replaces the roles and groups with the posted ones (missing lists are saved as empty)
                var staysAdmin = (user.InverseThis2Roles ?? new List<User2Role>()).Any(a => adminRoleIds.Contains(a.This2Role)) ||
                                 (user.InverseThis2UserGroups ?? new List<User2Group>()).Any(a => adminGroupIds.Contains(a.This2UserGroup));

                if (isAdmin && !staysAdmin)
                {
                    ret.Add(user.ObjId);
                }
            }

            return ret;
        }

        private static bool HasPasswordMismatch(User user)
        {
            return !String.IsNullOrEmpty(user.Password) && user.Password != user.PasswordConfirm;
        }

        /// <returns>null if no new password is set or it fulfills the password policy</returns>
        private static string GetPasswordPolicyError(User user)
        {
            return String.IsNullOrEmpty(user.Password) ? null : PasswordPolicy.Validate(user.Password, user.UserName);
        }

        private async Task SaveUserInternal(User user)
        {
            // everything that is derived from the posted object is computed once: the execution strategy runs the
            // lambda again on a transient error and must neither hash the hash nor lose the posted groups and roles
            var userId = user.ObjId;
            var groupIds = (user.InverseThis2UserGroups ?? new List<User2Group>()).Select(o => o.This2UserGroup).Distinct().ToList();
            var roleIds = (user.InverseThis2Roles ?? new List<User2Role>()).Select(o => o.This2Role).Distinct().ToList();
            var newPassword = user.Password;
            var mustChangePassword = user.MustChangePassword;
            var newSalt = UserHelper.GenerateNewSalt();
            var newPasswordHash = !String.IsNullOrEmpty(newPassword)
                // checked by the caller: Password == PasswordConfirm
                ? UserHelper.CreatePasswordHash(newPassword, newSalt)
                // only used for a new user without password: a random (hashed) password, login is not possible until one is set
                : UserHelper.CreatePasswordHash(UserHelper.GenerateNewSalt(), newSalt);

            var strategy = DbContext.Database.CreateExecutionStrategy();
            var attempt = 0;
            await strategy.ExecuteAsync(async
                () =>
            {
                if (attempt++ > 0)
                {
                    // retry: forget the entities tracked by the failed attempt
                    DbContext.ChangeTracker.Clear();
                }
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var existing = DbContext.Users.SingleOrDefault(a => a.ObjId == userId);
                    var target = existing ?? new User
                    {
                        ObjId = userId,
                        CreatedAt = DateTimeOffset.Now
                    };

                    // only the editable fields, never server side fields like the creation date
                    target.UserName = user.UserName;
                    target.FirstName = user.FirstName ?? "";
                    target.LastName = user.LastName ?? "";
                    target.Description = user.Description ?? "";
                    target.ModifiedAt = DateTimeOffset.Now;

                    if (!String.IsNullOrEmpty(newPassword) || existing == null)
                    {
                        target.Password = newPasswordHash;
                        target.Salt = newSalt;
                        // revokes the tokens issued with the old password
                        target.SecurityStamp = UserHelper.NewSecurityStamp();
                        // an administrator may require the user to choose an own password
                        target.MustChangePassword = mustChangePassword;
                    }

                    if (existing == null)
                    {
                        DbContext.Users.Add(target);
                    }

                    foreach (var groupId in groupIds)
                    {
                        var user2GroupExisting = DbContext.User2Groups.SingleOrDefault(a =>
                            a.This2User == userId && a.This2UserGroup == groupId);

                        if (user2GroupExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Groups.Add(new User2Group { This2User = userId, This2UserGroup = groupId });
                    }

                    var removedUserGroupsList = DbContext.User2Groups
                        .Where(c => !groupIds.Contains(c.This2UserGroup) && c.This2User == userId)
                        .ToList();
                    DbContext.RemoveRange(removedUserGroupsList);


                    foreach (var roleId in roleIds)
                    {
                        var rolesExisting = DbContext.User2Roles.SingleOrDefault(a =>
                            a.This2User == userId && a.This2Role == roleId);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Roles.Add(new User2Role { This2User = userId, This2Role = roleId });
                    }


                    var removedUserRolesList = DbContext.User2Roles
                        .Where(c => !roleIds.Contains(c.This2Role) && c.This2User == userId)
                        .ToList();
                    DbContext.RemoveRange(removedUserRolesList);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }
            });

            // the saved entity must not stay tracked with its hash (later saves of this request)
            DbContext.ChangeTracker.Clear();

            // after the commit and never the posted object (it carries the password)
            CacheSanitizedUser(userId);
        }

        private void CacheSanitizedUser(Guid userId)
        {
            var saved = DbContext.Users.AsNoTracking()
                .Include(a => a.InverseThis2Roles)
                .Include(a => a.InverseThis2UserGroups)
                .SingleOrDefault(a => a.ObjId == userId);

            _userCache.Remove(userId);

            if (saved != null)
            {
                saved.Password = null;
                saved.PasswordConfirm = null;
                saved.Salt = null;
                _userCache.Add(saved.ObjId, saved);
            }
        }


        [HttpPost]
        [Route("users")]
        public async Task<ActionResult<ICollection<User>>> SaveUsers([FromBody] IList<User> users)
        {
            if (users == null)
            {
                return BadRequest();
            }

            if (users.Any(HasPasswordMismatch))
            {
                return BadRequest("Password and password confirmation do not match");
            }
            var policyError = users.Select(GetPasswordPolicyError).FirstOrDefault(a => a != null);
            if (policyError != null)
            {
                return BadRequest(policyError);
            }

            // an empty list is treated as "nothing to delete"
            var removedIds = new List<Guid>();
            if (users.Count > 0)
            {
                var postedIds = users.Select(o => o.ObjId).ToList();
                removedIds = DbContext.Users.AsNoTracking().Where(c => !postedIds.Contains(c.ObjId)).Select(a => a.ObjId).ToList();
            }

            var guardError = CheckUserRemoval(removedIds) ?? CheckUsers(users, removedIds);
            if (guardError != null)
            {
                return BadRequest(guardError);
            }

            foreach (var user in users)
            {
                await SaveUserInternal(user);
            }

            if (removedIds.Count > 0)
            {
                var strategy = DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async
                    () =>
                {
                    await using var transaction = await DbContext.Database.BeginTransactionAsync();

                    try
                    {
                        var removedList = DbContext.Users.Where(c => removedIds.Contains(c.ObjId)).ToList();
                        DbContext.RemoveRange(removedList);

                        await DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Could not save data");
                        throw;
                    }
                });
            }

            _userCache.Clear();
            InvalidateUserClaims();
            return Ok(GetUsers());
        }

        /// <returns>an error message if the users must not be removed (current user or the last administrator)</returns>
        private string CheckUserRemoval(ICollection<Guid> removedUserIds)
        {
            if (removedUserIds.Count == 0)
            {
                return null;
            }

            var currentUserName = HttpContext?.User?.Identity?.Name;
            if (!String.IsNullOrEmpty(currentUserName) &&
                DbContext.Users.AsNoTracking().Any(a => removedUserIds.Contains(a.ObjId) && a.UserName == currentUserName))
            {
                return "Cannot delete the current user";
            }

            if (WouldRemoveLastAdmin(removedUserIds, Array.Empty<Guid>()))
            {
                return "Cannot delete the last administrator";
            }

            return null;
        }

        /// <summary>
        /// true if there is an administrator (direct role or through a user group) now, but none would be left
        /// after removing the given users and user groups
        /// </summary>
        private bool WouldRemoveLastAdmin(ICollection<Guid> removedUserIds, ICollection<Guid> removedGroupIds)
        {
            if (removedUserIds.Count == 0 && removedGroupIds.Count == 0)
            {
                return false;
            }

            var adminRoleIds = DbContext.Roles.AsNoTracking().Where(a => a.Key == Role.AdminRole).Select(a => a.ObjId).ToList();

            var directAdmins = DbContext.User2Roles.AsNoTracking()
                .Where(a => adminRoleIds.Contains(a.This2Role))
                .Select(a => new { a.This2User, This2UserGroup = (Guid?)null });
            var adminGroupIds = DbContext.UserGroup2Roles.AsNoTracking()
                .Where(a => adminRoleIds.Contains(a.This2Role))
                .Select(a => a.This2UserGroup)
                .ToList();
            var groupAdmins = DbContext.User2Groups.AsNoTracking()
                .Where(a => adminGroupIds.Contains(a.This2UserGroup))
                .Select(a => new { a.This2User, This2UserGroup = (Guid?)a.This2UserGroup });

            var admins = directAdmins.ToList().Concat(groupAdmins.ToList()).ToList();
            if (admins.Count == 0)
            {
                return false;
            }

            return !admins.Any(a => !removedUserIds.Contains(a.This2User) &&
                                    (!a.This2UserGroup.HasValue || !removedGroupIds.Contains(a.This2UserGroup.Value)));
        }
    }
}
