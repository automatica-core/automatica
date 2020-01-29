using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class UserCache : AbstractCache<User>, IUserCache
    {
        public UserCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<User> GetAll(AutomaticaContext context)
        {
            var data = context.Users.AsNoTracking().Include(a => a.InverseThis2Roles).Include(a => a.InverseThis2UserGroups).ToList();

            data.ForEach(a =>
            {
                a.Password = null;
                a.PasswordConfirm = null;
            });

            return data.AsQueryable();
        }

        protected override Guid GetKey(User obj)
        {
            return obj.ObjId;
        }
    }
}
