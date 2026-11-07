using System;
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
        private bool _loading;
        private bool _loadFailed;
        // after a failed load (e.g. database not reachable) the next attempt waits a bit, so callers don't all hit the db connect timeout
        private static readonly TimeSpan LoadRetryDelay = TimeSpan.FromSeconds(10);
        private DateTime _lastFailedLoad = DateTime.MinValue;
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
                if (!_initialized && DateTime.UtcNow - _lastFailedLoad >= LoadRetryDelay)
                {
                    _loadFailed = false;
                    _loading = true;
                    try
                    {
                        using (var db = new AutomaticaContext(_configuration))
                        {
                            foreach (var item in GetAll(db))
                            {
                                Add(GetKey(item), item);
                            }
                        }
                    }
                    finally
                    {
                        _loading = false;
                    }

                    // a failed load is retried on the next access instead of keeping an empty cache forever
                    _initialized = !_loadFailed;
                    _lastFailedLoad = _loadFailed ? DateTime.UtcNow : DateTime.MinValue;
                }
            }

            return base.All();
        }

        public override T2 Get(T1 key)
        {
            Initialize();

            return base.Get(key);
        }

        /// <summary>
        /// Loads the cache like <see cref="Get"/>, otherwise a cleared cache answers false until something else loads it
        /// (e.g. a category page after the category cache was cleared). Not while loading: Add checks Contains.
        /// </summary>
        public override bool Contains(T1 key)
        {
            if (!_initialized && !_loading)
            {
                Initialize();
            }

            return base.Contains(key);
        }

        protected abstract IQueryable<T2> GetAll(AutomaticaContext context);

        /// <summary>
        /// Called from <see cref="GetAll"/> when loading failed, the cache is not marked as initialized and loads again on the next access
        /// </summary>
        protected void MarkLoadFailed()
        {
            _loadFailed = true;
        }
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
                _lastFailedLoad = DateTime.MinValue;
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
