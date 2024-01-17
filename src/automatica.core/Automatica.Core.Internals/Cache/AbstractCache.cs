﻿using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache
{
    public abstract class AbstractCache<T1, T2> : StoreBase<T1, T2>
    {
        private readonly IConfiguration _configuration;
        private bool _initialized = false;
        private readonly object _lock = new object();

        public bool Initialized => _initialized;

        protected AbstractCache(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override ICollection<T2> All()
        {
            lock (_lock)
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

                    _initialized = true;
                }
            }

            return base.All();
        }

        public override T2 Get(T1 key)
        {
            Initialize();

            return base.Get(key);
        }

        protected abstract IQueryable<T2> GetAll(AutomaticaContext context);
        protected abstract T1 GetKey(T2 obj);

        protected void Initialize()
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    All();
                }
            }
        }

        public override void Clear()
        {
            lock (_lock)
            {
                _initialized = false;
                base.Clear();
            }
        }
    }

    public abstract class AbstractCache<T> : AbstractCache<Guid, T>
    {
        protected AbstractCache(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
