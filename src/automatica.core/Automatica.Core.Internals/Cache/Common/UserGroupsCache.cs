using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class UserGroupsCache : AbstractCache<UserGroup>, IUserGroupsCache
    {
        public UserGroupsCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<UserGroup> GetAll(AutomaticaContext context)
        {
            return context.UserGroups.AsNoTracking().Include(a => a.InverseThis2Roles).Include(a => a.InverseThis2Users);
        }

        protected override Guid GetKey(UserGroup obj)
        {
            return obj.ObjId;
        }
    }
}
