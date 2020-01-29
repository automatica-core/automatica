using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class CategoryCache : AbstractCache<CategoryInstance>, ICategoryCache
    {
        public CategoryCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<CategoryInstance> GetAll(AutomaticaContext context)
        {
            return context.CategoryInstances.AsNoTracking();
        }

        protected override Guid GetKey(CategoryInstance obj)
        {
            return obj.ObjId;
        }
    }
}
