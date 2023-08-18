using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder.Graphite
{
    internal class GraphiteDataRecorderWriter : BaseDataRecorderWriter
    {
        public GraphiteDataRecorderWriter(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.GraphiteRecorder, "GraphiteDataRecorderWriter", nodeCache, dispatcher, factory)
        {
        }


        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            return Task.CompletedTask;

        }
    }
}
