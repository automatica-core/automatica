﻿using System;
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
        public async Task DeleteUserGroup(Guid id)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();

                try
                {
                    var userGroup = DbContext.UserGroups.SingleOrDefault(a => a.ObjId == id);
                    if (userGroup != null)
                    {
                        DbContext.UserGroups.Remove(userGroup);
                        await DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _userGroupsCache.Clear();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not delete usergroup");
                    await transaction.RollbackAsync();
                }
            });
        }

        [HttpPost]
        [Route("usergroup")]
        public async Task<UserGroup> SaveUserGroup([FromBody] UserGroup instance)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    var existing = DbContext.UserGroups.SingleOrDefault(a => a.ObjId == instance.ObjId);
                    var roles = instance.InverseThis2Roles;
                    instance.InverseThis2Roles = null;

                    if (existing == null)
                    {
                        DbContext.UserGroups.Add(instance);
                    }
                    else
                    {
                        DbContext.Entry(existing).State = EntityState.Detached;
                        DbContext.UserGroups.Update(instance);
                    }

                    foreach (var role in roles)
                    {
                        var rolesExisting = DbContext.UserGroup2Roles.SingleOrDefault(a =>
                            a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        DbContext.UserGroup2Roles.Add(role);
                    }

                    if (instance.InverseThis2Roles != null)
                    {
                        var removedUserRoles = from c in DbContext.UserGroup2Roles
                            where !(from o in instance.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                            select c;

                        var removedUserRolesList = removedUserRoles.ToList();
                        DbContext.RemoveRange(removedUserRolesList);
                    }

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _userGroupsCache.Clear();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                }

                return GetUserGroup(instance.ObjId);
            });
        }


        [HttpPost]
        [Route("usergroups")]
        public async Task<ICollection<UserGroup>> SaveUserGroups([FromBody]IList<UserGroup> instances)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var instance in instances)
                    {
                        var existing = DbContext.UserGroups.SingleOrDefault(a => a.ObjId == instance.ObjId);
                        var roles = instance.InverseThis2Roles;
                        instance.InverseThis2Roles = null;

                        if (existing == null)
                        {
                            DbContext.UserGroups.Add(instance);
                        }
                        else
                        {
                            DbContext.Entry(existing).State = EntityState.Detached;
                            DbContext.UserGroups.Update(instance);
                        }

                        foreach (var role in roles)
                        {
                            var rolesExisting = DbContext.UserGroup2Roles.SingleOrDefault(a =>
                                a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

                            if (rolesExisting != null)
                            {
                                continue;
                            }

                            DbContext.UserGroup2Roles.Add(role);
                        }

                        if (instance.InverseThis2Roles != null)
                        {
                            var removedUserRoles = from c in DbContext.UserGroup2Roles
                                where !(from o in instance.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                                select c;

                            var removedUserRolesList = removedUserRoles.ToList();
                            DbContext.RemoveRange(removedUserRolesList);
                        }
                    }

                    var removedNodes = from c in DbContext.UserGroups
                        where !(from o in instances select o.ObjId).Contains(c.ObjId)
                        select c;
                    var removedList = removedNodes.ToList();
                    DbContext.RemoveRange(removedList);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _userGroupsCache.Clear();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                }

                return GetUserGroups();
            });
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
        public async Task DeleteUser(Guid id)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();

                try
                {
                    var user = DbContext.Users.SingleOrDefault(a => a.ObjId == id);
                    if (user != null)
                    {
                        DbContext.Users.Remove(user);
                        await DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _userCache.Clear();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not delete user");
                    await transaction.RollbackAsync();
                }
            });
        }

        [HttpPost]
        [Route("user")]
        public async Task<User> SaveUser([FromBody] User user)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var user2Groups = user.InverseThis2UserGroups;
                    var roles = user.InverseThis2Roles;
                    user.InverseThis2UserGroups = null;
                    user.InverseThis2Roles = null;
                    user.CreatedAt = DateTimeOffset.Now;


                    var existing = DbContext.Users.SingleOrDefault(a => a.ObjId == user.ObjId);

                    if (user.Password != null && user.Password == user.PasswordConfirm)
                    {
                        var salt = UserHelper.GenerateNewSalt();
                        user.Password = UserHelper.HashPassword(user.Password, salt);
                        user.Salt = salt;
                        user.PasswordConfirm = null;
                    }
                    else if (existing != null)
                    {
                        user.Password = existing.Password;
                        user.Salt = existing.Salt;

                        user.ModifiedAt = DateTimeOffset.Now;
                    }

                    if (existing == null)
                    {
                        if (String.IsNullOrEmpty(user.Password))
                        {
                            user.Password = UserHelper.GenerateNewSalt();
                            user.Salt = UserHelper.GenerateNewSalt();
                        }

                        DbContext.Users.Add(user);
                    }
                    else
                    {
                        DbContext.Entry(existing).State = EntityState.Detached;
                        DbContext.Users.Update(user);
                    }

                    foreach (var user2Group in user2Groups)
                    {
                        var user2GroupExisting = DbContext.User2Groups.SingleOrDefault(a =>
                            a.This2User == user2Group.This2User && a.This2UserGroup == user2Group.This2UserGroup);

                        if (user2GroupExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Groups.Add(user2Group);
                    }

                    var removedUserGroups = from c in DbContext.User2Groups
                        where !(from o in user2Groups select o.This2UserGroup).Contains(c.This2UserGroup) &&
                              c.This2User == user.ObjId
                        select c;

                    var removedUserGroupsList = removedUserGroups.ToList();
                    DbContext.RemoveRange(removedUserGroupsList);


                    foreach (var role in roles)
                    {
                        var rolesExisting = DbContext.User2Roles.SingleOrDefault(a =>
                            a.This2User == role.This2User && a.This2Role == role.This2Role);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Roles.Add(role);
                    }


                    var removedUserRoles = from c in DbContext.User2Roles
                        where !(from o in roles select o.This2Role).Contains(c.This2Role) &&
                              c.This2User == user.ObjId
                        select c;

                    var removedUserRolesList = removedUserRoles.ToList();
                    DbContext.RemoveRange(removedUserRolesList);


                    _userCache.Clear();
                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                }

                return GetUser(user.ObjId);
            });
        }


        [HttpPost]
        [Route("users")]
        public async Task<ICollection<UserGroup>> SaveUsers([FromBody] IList<User> users)
        {
            foreach (var user in users)
            {
                await SaveUser(user);
            }

            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();

                try
                {
                    var removedNodes = from c in DbContext.Users
                        where !(from o in users select o.ObjId).Contains(c.ObjId)
                        select c;
                    var removedList = removedNodes.ToList();
                    DbContext.RemoveRange(removedList);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _userCache.Clear();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                }

                return GetUserGroups();
            });
        }
    }
}
