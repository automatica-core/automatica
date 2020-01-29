using System;
using System.IO;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;

namespace Automatica.Core.Runtime.Recorder
{
    internal class FileDataRecorderWriter : BaseDataRecorderWriter
    {
        private readonly object _lock = new object();
        public FileDataRecorderWriter(INodeInstanceCache nodeCache, IDispatcher dispatcher) : base("FileDataRecorder", nodeCache, dispatcher)
        {
            if (!Directory.Exists(ServerInfo.GetTrendingDirectory()))
            {
                Directory.CreateDirectory(ServerInfo.GetTrendingDirectory());
            }
        }

        internal override void Save(Trending trend, NodeInstance nodeInstance)
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
        }
    }
}
