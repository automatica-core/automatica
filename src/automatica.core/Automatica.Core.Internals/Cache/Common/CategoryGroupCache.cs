using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class CategoryGroupCache : AbstractCache<CategoryGroup>, ICategoryGroupCache
    {
        public CategoryGroupCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<CategoryGroup> GetAll(AutomaticaContext context)
        {
            return context.CategoryGroups.AsNoTracking();
        }

        protected override Guid GetKey(CategoryGroup obj)
        {
            return obj.ObjId;
        }
    }
}
