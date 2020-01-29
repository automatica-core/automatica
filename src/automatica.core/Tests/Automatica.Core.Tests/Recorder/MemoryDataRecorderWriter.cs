using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder;

namespace Automatica.Core.Tests.Recorder
{
    internal class MemoryDataRecorderWriter : BaseDataRecorderWriter
    {
        public Trending LastTrending { get; private set; }

        public MemoryDataRecorderWriter(string recorderName, INodeInstanceCache nodeCache, IDispatcher dispatcher) : base(recorderName, nodeCache, dispatcher)
        {
        }

        internal override void Save(Trending trend, NodeInstance nodeInstance)
        {
            LastTrending = trend;
        }
    }
}
