using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Automatica.Core.Runtime.Recorder.FileSystem
{
    internal class FileDataRecorderWriter : BaseDataRecorderWriter
    {
        private readonly object _lock = new object();
        public FileDataRecorderWriter(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.FileRecorder, "FileDataRecorder", nodeCache, dispatcher, factory)
        {
            if (!Directory.Exists(ServerInfo.GetTrendingDirectory()))
            {
                Directory.CreateDirectory(ServerInfo.GetTrendingDirectory());
            }
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            var fileName = Path.Combine(ServerInfo.GetTrendingDirectory(), nodeInstance.ObjId + ".csv");
            lock (_lock)
            {
                var isNewFile = !File.Exists(fileName);

                using var file =
                    File.Open(fileName, FileMode.Append);
                using var streamWriter = new StreamWriter(file);

                if (isNewFile)
                {
                    streamWriter.WriteLine("Id,Name,Timestamp,Value,Source");
                }

                streamWriter.WriteLine($"{nodeInstance.ObjId},{nodeInstance.Name},{trend.TimestampIso},{trend.Value},{trend.Source}");
            }

            return Task.CompletedTask;
        }
    }
}
