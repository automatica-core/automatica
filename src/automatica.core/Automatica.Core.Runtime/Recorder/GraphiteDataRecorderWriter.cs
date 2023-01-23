using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder
{
    internal class GraphiteDataRecorderWriter : BaseDataRecorderWriter
    {
        public GraphiteDataRecorderWriter(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.GraphiteRecorder, "GraphiteDataRecorderWriter", nodeCache, dispatcher, factory)
        {
        }
        

        internal override void Save(Trending trend, NodeInstance nodeInstance)
        {

        
        }
    }
}
