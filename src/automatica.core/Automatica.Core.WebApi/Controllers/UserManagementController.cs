using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            }
            return Ok();
        }

        [HttpPost]
        [Route("usergroup")]
        public async Task<UserGroup> SaveUserGroup([FromBody] UserGroup instance)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
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
            return GetUserGroup(instance.ObjId);
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

            if (WouldRemoveLastAdmin(Array.Empty<Guid>(), removedIds))
            {
                return BadRequest("Cannot delete the last user group with administrator rights");
            }

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
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

            await SaveUserInternal(user);
            return GetUser(user.ObjId);
        }

        private static bool HasPasswordMismatch(User user)
        {
            return !String.IsNullOrEmpty(user.Password) && user.Password != user.PasswordConfirm;
        }

        private async Task SaveUserInternal(User user)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var user2Groups = user.InverseThis2UserGroups ?? new List<User2Group>();
                    var roles = user.InverseThis2Roles ?? new List<User2Role>();
                    user.InverseThis2UserGroups = null;
                    user.InverseThis2Roles = null;


                    var existing = DbContext.Users.AsNoTracking().SingleOrDefault(a => a.ObjId == user.ObjId);

                    if (!String.IsNullOrEmpty(user.Password))
                    {
                        // checked by the caller: Password == PasswordConfirm
                        var salt = UserHelper.GenerateNewSalt();
                        user.Password = UserHelper.HashPassword(user.Password, salt);
                        user.Salt = salt;
                    }
                    else if (existing != null)
                    {
                        user.Password = existing.Password;
                        user.Salt = existing.Salt;
                    }
                    else
                    {
                        // new user without password: a random (hashed) password, login is not possible until one is set
                        var salt = UserHelper.GenerateNewSalt();
                        user.Password = UserHelper.HashPassword(UserHelper.GenerateNewSalt(), salt);
                        user.Salt = salt;
                    }
                    user.PasswordConfirm = null;

                    if (existing == null)
                    {
                        user.CreatedAt = DateTimeOffset.Now;
                        user.ModifiedAt = DateTimeOffset.Now;
                        DbContext.Users.Add(user);
                    }
                    else
                    {
                        user.CreatedAt = existing.CreatedAt;
                        user.ModifiedAt = DateTimeOffset.Now;
                        DbContext.Users.Update(user);
                    }

                    foreach (var user2Group in user2Groups)
                    {
                        user2Group.This2User = user.ObjId;
                        user2Group.This2UserNavigation = null;
                        user2Group.This2UserGroupNavigation = null;

                        var user2GroupExisting = DbContext.User2Groups.SingleOrDefault(a =>
                            a.This2User == user2Group.This2User && a.This2UserGroup == user2Group.This2UserGroup);

                        if (user2GroupExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Groups.Add(user2Group);
                    }

                    var groupIds = user2Groups.Select(o => o.This2UserGroup).ToList();
                    var removedUserGroupsList = DbContext.User2Groups
                        .Where(c => !groupIds.Contains(c.This2UserGroup) && c.This2User == user.ObjId)
                        .ToList();
                    DbContext.RemoveRange(removedUserGroupsList);


                    foreach (var role in roles)
                    {
                        role.This2User = user.ObjId;
                        role.This2UserNavigation = null;
                        role.This2RoleNavigation = null;

                        var rolesExisting = DbContext.User2Roles.SingleOrDefault(a =>
                            a.This2User == role.This2User && a.This2Role == role.This2Role);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Roles.Add(role);
                    }


                    var roleIds = roles.Select(o => o.This2Role).ToList();
                    var removedUserRolesList = DbContext.User2Roles
                        .Where(c => !roleIds.Contains(c.This2Role) && c.This2User == user.ObjId)
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

            // after the commit and never the posted object (it carries the password hash)
            CacheSanitizedUser(user.ObjId);
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

            // an empty list is treated as "nothing to delete"
            var removedIds = new List<Guid>();
            if (users.Count > 0)
            {
                var postedIds = users.Select(o => o.ObjId).ToList();
                removedIds = DbContext.Users.AsNoTracking().Where(c => !postedIds.Contains(c.ObjId)).Select(a => a.ObjId).ToList();
            }

            var guardError = CheckUserRemoval(removedIds);
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
