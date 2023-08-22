using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Runtime.Recorder.Base;

namespace Automatica.Core.Runtime.Recorder.Memory
{
    internal class MemoryDataRecorder : BaseDataRecorderWriter
    {
        private readonly Dictionary<Guid, object> _trends = new();

        public MemoryDataRecorder(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.MemoryRecorder, nameof(MemoryDataRecorder), nodeCache, dispatcher, factory)

        {
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            _trends[trend.This2NodeInstance] = trend.Value;
            return Task.CompletedTask;
        }
    }
}
