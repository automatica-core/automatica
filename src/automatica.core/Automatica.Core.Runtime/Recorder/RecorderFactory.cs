using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder
{
    internal class RecorderFactory : IRecorderFactory
    {
        private readonly IDictionary<DataRecorderType, IDataRecorderWriter> _writers =
            new ConcurrentDictionary<DataRecorderType, IDataRecorderWriter>();

        public RecorderFactory(INodeInstanceCache nodeInstanceCache, IDispatcher dispatcher, ILoggerFactory loggerFactory, IConfiguration config, ISettingsCache settingsCache)
        {

            _writers.Add(DataRecorderType.CloudRecorder, new CloudDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.DatabaseRecorder, new DatabaseDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.FileRecorder, new FileDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.GraphiteRecorder, new GraphiteDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.HostedGrafanaRecorder, new HostedGrafanaDataRecorderWriter(config, settingsCache, nodeInstanceCache, dispatcher, loggerFactory));
        }

        public IDataRecorderWriter GetRecorder(DataRecorderType recorderType)
        {
            if (_writers.ContainsKey(recorderType))
            {
                return _writers[recorderType];
            }
            throw new NotImplementedException();
        }
    }
}
