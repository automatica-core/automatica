using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder.Cloud
{
    internal class CloudDataRecorderWriter : BaseDataRecorderWriter
    {
        public CloudDataRecorderWriter(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.CloudRecorder, "CloudDataRecorderWriter", nodeCache, dispatcher, factory)
        {
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            Logger.LogInformation($"CloudLogger save is not implemented...");
            return Task.CompletedTask;
        }
    }
}
