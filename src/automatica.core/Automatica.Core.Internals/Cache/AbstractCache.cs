using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache
{
    public abstract class AbstractCache<T> : GuidStoreBase<T>
    {
        private readonly IConfiguration _configuration;
        private bool _initialized = false;

        protected AbstractCache(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override ICollection<T> All()
        {
            if (!_initialized)
            {
                using (var db = new AutomaticaContext(_configuration))
                {
                    foreach (var item in GetAll(db))
                    {
                        Add(GetKey(item), item);
                    }
                }
            }
            return base.All();
        }

        protected abstract IQueryable<T> GetAll(AutomaticaContext context);
        protected abstract Guid GetKey(T obj);

        public override void Clear()
        {
            _initialized = false;
            base.Clear();
        }
    }
}
