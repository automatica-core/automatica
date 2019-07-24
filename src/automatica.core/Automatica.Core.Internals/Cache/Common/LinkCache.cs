using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class LinkCache : AbstractCache<Link>, ILinkCache
    {
        public LinkCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<Link> GetAll(AutomaticaContext context)
        {
            return context.Links.AsNoTracking();
        }

        protected override Guid GetKey(Link obj)
        {
            return obj.ObjId;
        }
    }
}
