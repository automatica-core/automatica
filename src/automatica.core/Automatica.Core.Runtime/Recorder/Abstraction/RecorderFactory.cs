using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.HyperSeries;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Base;
using Automatica.Core.Runtime.Recorder.Cloud;
using Automatica.Core.Runtime.Recorder.Database;
using Automatica.Core.Runtime.Recorder.FileSystem;
using Automatica.Core.Runtime.Recorder.Graphite;
using Automatica.Core.Runtime.Recorder.HostedGrafana;
using Automatica.Core.Runtime.Recorder.HyperSeries;
using Automatica.Core.Runtime.Recorder.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder.Abstraction
{
    internal class RecorderFactory : IRecorderFactory
    {
        private readonly IDictionary<DataRecorderType, IDataRecorderWriter> _writers =
            new ConcurrentDictionary<DataRecorderType, IDataRecorderWriter>();

        public RecorderFactory(INodeInstanceCache nodeInstanceCache, IDispatcher dispatcher, ILoggerFactory loggerFactory, IConfigurationRoot config, ISettingsCache settingsCache, IServiceProvider serviceProvider)
        {
            _writers.Add(DataRecorderType.CloudRecorder, new CloudDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.DatabaseRecorder, new DatabaseDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.FileRecorder, new FileDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.GraphiteRecorder, new GraphiteDataRecorderWriter(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.HostedGrafanaRecorder, new HostedGrafanaDataRecorderWriter(config, settingsCache, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.MemoryRecorder, new MemoryDataRecorder(config, nodeInstanceCache, dispatcher, loggerFactory));
            _writers.Add(DataRecorderType.HyperSeriesRecorder, new HyperSeriesRecorder(config, nodeInstanceCache, dispatcher, serviceProvider.GetRequiredService<IHyperSeriesRepository>(), loggerFactory));
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
