using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder
{
    internal class CloudDataRecorderWriter : BaseDataRecorderWriter
    {
        public CloudDataRecorderWriter(INodeInstanceCache nodeCache, IDispatcher dispatcher) : base("CloudDataRecorderWriter", nodeCache, dispatcher)
        {
        }

        internal override void Save(Trending trend)
        {
            Logger.LogInformation($"CloudLogger save is not implemented...");
        }
    }
}
