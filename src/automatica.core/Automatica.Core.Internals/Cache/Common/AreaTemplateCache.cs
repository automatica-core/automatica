using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class AreaTemplateCache : AbstractCache<AreaTemplate>, IAreaTemplateCache
    {
        public AreaTemplateCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<AreaTemplate> GetAll(AutomaticaContext context)
        {
            return context.AreaTemplates.AsNoTracking();
        } 

        protected override Guid GetKey(AreaTemplate obj)
        {
            return obj.ObjId;
        }
    }
}
