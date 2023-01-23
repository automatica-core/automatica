using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder
{
    internal class DatabaseDataRecorderWriter : BaseDataRecorderWriter
    {
        private readonly DatabaseTrendingValueStore _databaseTrendingValueStore;

        public DatabaseDataRecorderWriter(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher,ILoggerFactory factory) : base(config, DataRecorderType.DatabaseRecorder, "DatabaseDataRecorderWriter", nodeCache, dispatcher, factory)
        {
            _databaseTrendingValueStore = new DatabaseTrendingValueStore(config, factory.CreateLogger("Trending"));
        }

        internal override void Save(Trending trend, NodeInstance nodeInstance)
        {
            _databaseTrendingValueStore.Add(trend, nodeInstance);
        }
    }
}
