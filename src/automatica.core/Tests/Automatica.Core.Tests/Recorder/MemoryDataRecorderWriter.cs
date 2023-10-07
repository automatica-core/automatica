using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Tests.Recorder
{
    internal class MemoryDataRecorderWriter : BaseDataRecorderWriter
    {
        public Trending LastTrending { get; private set; }

        public MemoryDataRecorderWriter(IConfiguration config, string recorderName, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.MemoryRecorder, recorderName, nodeCache, dispatcher, factory)
        {
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            LastTrending = trend;
            return Task.CompletedTask;
        }
    }
}
