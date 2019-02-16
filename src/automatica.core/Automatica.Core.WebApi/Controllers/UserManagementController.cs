using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("usermgm")]
    [Authorize(Policy = Role.AdminRole)]
    public class UserManagementController : BaseController
    {
        public UserManagementController(AutomaticaContext dbContext) : base(dbContext)
        {
        }


        [HttpGet]
        [Route("usergroups")]
        public IList<UserGroup> GetUserGroups()
        {
            return DbContext.UserGroups.Include(a => a.InverseThis2Roles).Include(a => a.InverseThis2Users).ToList();
        }

        [HttpPost]
        [Route("usergroups")]
        public IList<UserGroup> SaveUserGroups([FromBody]IList<UserGroup> instances)
        {
            var transaction = DbContext.Database.BeginTransaction();
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
                        var rolesExisting = DbContext.UserGroup2Roles.SingleOrDefault(a => a.This2UserGroup == role.This2UserGroup && a.This2Role == role.This2Role);

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

                        var emovedUserRolesList = removedUserRoles.ToList();
                        DbContext.RemoveRange(emovedUserRolesList);
                    }
                }

                var removedNodes = from c in DbContext.UserGroups
                    where !(from o in instances select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedList = removedNodes.ToList();
                DbContext.RemoveRange(removedList);

                DbContext.SaveChanges();
                transaction.Commit();
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
        public IList<User> GetUsers()
        {
            var data = DbContext.Users.Include(a => a.InverseThis2Roles).Include(a => a.InverseThis2UserGroups).ToList();

            data.ForEach(a =>
            {
                a.Password = null;
                a.PasswordConfirm = null;
            });

            return data;
        }

        [HttpGet]
        [Route("roles")]
        public IList<Role> GetRoles()
        {
            return DbContext.Roles.ToList();
        }

        [HttpPost]
        [Route("users")]
        public IList<UserGroup> SaveUsers([FromBody]IList<User> users)
        {
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                foreach (var user in users)
                {

                    var user2Groups = user.InverseThis2UserGroups;
                    var roles = user.InverseThis2Roles;
                    user.InverseThis2UserGroups = null;
                    user.InverseThis2Roles = null;


                    var existing = DbContext.Users.SingleOrDefault(a => a.ObjId == user.ObjId);

                    if (user.Password != null && user.Password == user.PasswordConfirm)
                    {
                        var salt = UserHelper.GenerateNewSalt();
                        user.Password = UserHelper.HashPassword(user.Password, salt);
                        user.Salt = salt;
                        user.PasswordConfirm = null;
                    }
                    else if(existing != null)
                    {
                        user.Password = existing.Password;
                        user.Salt = existing.Salt;
                    }

                    if (existing == null)
                    {
                        DbContext.Users.Add(user);
                    }
                    else
                    {
                        DbContext.Entry(existing).State = EntityState.Detached;
                        DbContext.Users.Update(user);
                    }

                    foreach(var user2Group in user2Groups)
                    {
                        var user2GroupExisting = DbContext.User2Groups.SingleOrDefault(a => a.This2User == user2Group.This2User && a.This2UserGroup == user2Group.This2UserGroup);

                        if(user2GroupExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Groups.Add(user2Group);
                    }

                    if (user.InverseThis2UserGroups != null)
                    {
                        var removedUserGroups = from c in DbContext.User2Groups
                                                where !(from o in user.InverseThis2UserGroups select o.This2UserGroup).Contains(c.This2UserGroup)
                                                select c;

                        var removedUserGroupsList = removedUserGroups.ToList();
                        DbContext.RemoveRange(removedUserGroupsList);
                    }

                    foreach (var role in roles)
                    {
                        var rolesExisting = DbContext.User2Roles.SingleOrDefault(a => a.This2User == role.This2User && a.This2Role == role.This2Role);

                        if (rolesExisting != null)
                        {
                            continue;
                        }

                        DbContext.User2Roles.Add(role);
                    }

                    if (user.InverseThis2Roles != null)
                    {
                        var removedUserRoles = from c in DbContext.User2Roles
                                                where !(from o in user.InverseThis2Roles select o.This2Role).Contains(c.This2Role)
                                                select c;

                        var emovedUserRolesList = removedUserRoles.ToList();
                        DbContext.RemoveRange(emovedUserRolesList);
                    }

                }

                var removedNodes = from c in DbContext.Users
                                   where !(from o in users select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedList = removedNodes.ToList();
                DbContext.RemoveRange(removedList);

                DbContext.SaveChanges();
                transaction.Commit();
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
