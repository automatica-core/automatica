using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Database;

namespace Automatica.Core.Runtime.Recorder
{
    internal class DatabaseDataRecorderWriter : BaseDataRecorderWriter
    {
        private readonly DatabaseTrendingValueStore _databaseTrendingValueStore;

        public DatabaseDataRecorderWriter(INodeInstanceCache nodeCache, IDispatcher dispatcher, DatabaseTrendingValueStore databaseTrendingValueStore) : base("DatabaseDataRecorderWriter", nodeCache, dispatcher)
        {
            _databaseTrendingValueStore = databaseTrendingValueStore;
        }

        internal override void Save(Trending trend, NodeInstance nodeInstance)
        {
            _databaseTrendingValueStore.Add(trend, nodeInstance);
        }
    }
}
