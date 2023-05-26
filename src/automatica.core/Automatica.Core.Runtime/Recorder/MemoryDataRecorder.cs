using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;

namespace Automatica.Core.Runtime.Recorder
{
    internal class MemoryDataRecorder : BaseDataRecorderWriter
    {
        private readonly Dictionary<Guid, object> _trends = new();

        public MemoryDataRecorder(IConfiguration config, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory) : base(config, DataRecorderType.FileRecorder, "FileDataRecorder", nodeCache, dispatcher, factory)
       
        {
        }

        internal override void Save(Trending trend, NodeInstance nodeInstance)
        {
            if (!_trends.ContainsKey(trend.This2NodeInstance))
            {
                _trends.Add(trend.This2NodeInstance, null);
            }
            _trends[trend.This2NodeInstance] = trend.Value;
        }

    }
}
