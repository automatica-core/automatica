using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Control
{
    internal class ControlCache : AbstractCache<IControl>, IControlCache
    {
        
        public ControlCache(IConfiguration configuration) : base(configuration)
        {
        }


        protected override IQueryable<IControl> GetAll(AutomaticaContext context)
        {
            if (Initialized)
            {
                return All().AsQueryable();
            }
            return new List<IControl>().AsQueryable();
        }

        protected override Guid GetKey(IControl obj)
        {
            return obj.Id;
        }
    }
}
