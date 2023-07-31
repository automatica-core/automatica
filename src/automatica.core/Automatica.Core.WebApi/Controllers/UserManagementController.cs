using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cloud.Model;
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
        private readonly IUserCache _userCache;
        private readonly IUserGroupsCache _userGroupsCache;
        private readonly IConfiguration _config;

        public UserManagementController(AutomaticaContext dbContext, IUserCache userCache, IUserGroupsCache userGroupsCache, IConfiguration config) : base(dbContext)
        {
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
        public void DeleteUserGroup(Guid id)
        {
            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                var userGroup = dbContext.UserGroups.SingleOrDefault(a => a.ObjId == id);
                if (userGroup != null)
                {
                    dbContext.UserGroups.Remove(userGroup);
                    transaction.Commit();
                    dbContext.SaveChanges();
                    _userGroupsCache.Clear();
                }
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not delete usergroup");
                transaction.Rollback();
            }
        }

        [HttpPost]
        [Route("usergroup")]
        public UserGroup SaveUserGroup([FromBody] UserGroup instance)
        {
            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();
            try
            {

                var existing = dbContext.UserGroups.SingleOrDefault(a => a.ObjId == instance.ObjId);
                var roles = instance.InverseThis2Roles;
                instance.InverseThis2Roles = null;

                if (existing == null)
                {
                    dbContext.UserGroups.Add(instance);
                }
                else
                {
                    dbContext.Entry(existing).State = EntityState.Detached;
                    dbContext.UserGroups.Update(instance);
                }

                foreach (var role in roles)
                {
                    var rolesExisting = dbContext.UserGroup2Roles.SingleOrDefault(a =>
                        a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

                    if (rolesExisting != null)
                    {
                        continue;
                    }

                    dbContext.UserGroup2Roles.Add(role);
                }

                if (instance.InverseThis2Roles != null)
                {
                    var removedUserRoles = from c in dbContext.UserGroup2Roles
                        where !(from o in instance.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                        select c;

                    var removedUserRolesList = removedUserRoles.ToList();
                    dbContext.RemoveRange(removedUserRolesList);
                }

                dbContext.SaveChanges();
                transaction.Commit();
                _userGroupsCache.Clear();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetUserGroup(instance.ObjId);
        }


        [HttpPost]
        [Route("usergroups")]
        public ICollection<UserGroup> SaveUserGroups([FromBody]IList<UserGroup> instances)
        {
            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();
            try
            {
                foreach (var instance in instances)
                {
                    var existing = dbContext.UserGroups.SingleOrDefault(a => a.ObjId == instance.ObjId);
                    var roles = instance.InverseThis2Roles;
                    instance.InverseThis2Roles = null;

                    if (existing == null)
                    {
                        dbContext.UserGroups.Add(instance);
                    }
                    else
                    {
                        dbContext.Entry(existing).State = EntityState.Detached;
                        dbContext.UserGroups.Update(instance);
                    }

                    foreach (var role in roles)
                    {
                        var rolesExisting = dbContext.UserGroup2Roles.SingleOrDefault(a => a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        dbContext.UserGroup2Roles.Add(role);
                    }

                    if (instance.InverseThis2Roles != null)
                    {
                        var removedUserRoles = from c in dbContext.UserGroup2Roles
                                               where !(from o in instance.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                                               select c;

                        var removedUserRolesList = removedUserRoles.ToList();
                        dbContext.RemoveRange(removedUserRolesList);
                    }
                }

                var removedNodes = from c in dbContext.UserGroups
                    where !(from o in instances select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedList = removedNodes.ToList();
                dbContext.RemoveRange(removedList);

                dbContext.SaveChanges();
                transaction.Commit();
                _userGroupsCache.Clear();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetUserGroups();
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
        public void DeleteUser(Guid id)
        {
            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                var user = dbContext.Users.SingleOrDefault(a => a.ObjId == id);
                if (user != null)
                {
                    dbContext.Users.Remove(user);
                    transaction.Commit();
                    dbContext.SaveChanges();
                    _userCache.Clear();
                }
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not delete user");
                transaction.Rollback();
            }
        }

        [HttpPost]
        [Route("user")]
        public User SaveUser([FromBody] User user)
        {
            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();
            try
            {
                var user2Groups = user.InverseThis2UserGroups;
                var roles = user.InverseThis2Roles;
                user.InverseThis2UserGroups = null;
                user.InverseThis2Roles = null;


                var existing = dbContext.Users.SingleOrDefault(a => a.ObjId == user.ObjId);

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
                }

                if (existing == null)
                {
                    if (String.IsNullOrEmpty(user.Password))
                    {
                        user.Password = UserHelper.GenerateNewSalt();
                        user.Salt = UserHelper.GenerateNewSalt();
                    }

                    dbContext.Users.Add(user);
                }
                else
                {
                    dbContext.Entry(existing).State = EntityState.Detached;
                    dbContext.Users.Update(user);
                }

                foreach (var user2Group in user2Groups)
                {
                    var user2GroupExisting = dbContext.User2Groups.SingleOrDefault(a => a.This2User == user2Group.This2User && a.This2UserGroup == user2Group.This2UserGroup);

                    if (user2GroupExisting != null)
                    {
                        continue;
                    }

                    dbContext.User2Groups.Add(user2Group);
                }

                if (user.InverseThis2UserGroups != null)
                {
                    var removedUserGroups = from c in dbContext.User2Groups
                                            where !(from o in user.InverseThis2UserGroups select o.This2UserGroup).Contains(c.This2UserGroup)
                                            select c;

                    var removedUserGroupsList = removedUserGroups.ToList();
                    dbContext.RemoveRange(removedUserGroupsList);
                }

                foreach (var role in roles)
                {
                    var rolesExisting = dbContext.User2Roles.SingleOrDefault(a => a.This2User == role.This2User && a.This2Role == role.This2Role);

                    if (rolesExisting != null)
                    {
                        continue;
                    }

                    dbContext.User2Roles.Add(role);
                }

                if (user.InverseThis2Roles != null)
                {
                    var removedUserRoles = from c in dbContext.User2Roles
                                           where !(from o in user.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                                           select c;

                    var removedUserRolesList = removedUserRoles.ToList();
                    dbContext.RemoveRange(removedUserRolesList);
                }
                _userCache.Clear();
                dbContext.SaveChanges();
                transaction.Commit();
            }
            catch(Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetUser(user.ObjId);
        }


        [HttpPost]
        [Route("users")]
        public ICollection<UserGroup> SaveUsers([FromBody] IList<User> users)
        {
            foreach (var user in users)
            {
                SaveUser(user);
            }

            using var dbContext = new AutomaticaContext(_config);
            var transaction = dbContext.Database.BeginTransaction();

            try
            {
                var removedNodes = from c in dbContext.Users
                    where !(from o in users select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedList = removedNodes.ToList();
                dbContext.RemoveRange(removedList);

                dbContext.SaveChanges();
                transaction.Commit();
                _userCache.Clear();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetUserGroups();
        }
    }
}
